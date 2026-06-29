using System.Globalization;
using System.Text.Json;

namespace RightKeyboard;

public sealed class Configuration
{
    private const int CurrentSchemaVersion = 3;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public Dictionary<string, DevicePreference> Devices { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Layout> LayoutMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> IgnoredDevices { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static Configuration LoadConfiguration(
        KeyboardDevicesCollection devices,
        string? configurationFile = null,
        IEnumerable<Layout>? availableLayouts = null)
    {
        string path = configurationFile ?? GetConfigFilePath();
        Layout[] layouts = (availableLayouts ?? Layout.EnumerateLayouts()).ToArray();

        if (File.Exists(path))
        {
            return LoadJson(path, layouts);
        }

        if (configurationFile is null && File.Exists(GetLegacyConfigFilePath()))
        {
            Configuration migrated = LoadLegacy(devices, layouts, GetLegacyConfigFilePath());
            migrated.Save(path);
            return migrated;
        }

        return new Configuration();
    }

    public DevicePreference TouchDevice(KeyboardDevice device, string? customName = null)
    {
        if (!Devices.TryGetValue(device.Identity, out DevicePreference? preference))
        {
            preference = new DevicePreference { Identity = device.Identity };
            Devices[device.Identity] = preference;
        }

        preference.Fingerprint = device.Fingerprint;
        preference.DetectedName = device.DisplayName;
        preference.TechnicalId = device.TechnicalId;
        preference.LastSeenUtc = DateTimeOffset.UtcNow;
        if (customName is not null)
        {
            preference.CustomName = NormalizeCustomName(customName, device.DisplayName);
        }

        return preference;
    }

    public string GetDisplayName(KeyboardDevice device) =>
        Devices.TryGetValue(device.Identity, out DevicePreference? preference)
            ? preference.DisplayName
            : BuildSuggestedName(device.DisplayName, device.TechnicalId);

    public bool TryGetLayout(
        KeyboardDevice device,
        int connectedDevicesWithSameFingerprint,
        out Layout? layout,
        out bool learnedIdentity)
    {
        learnedIdentity = false;
        TouchDevice(device);
        if (LayoutMappings.TryGetValue(device.Identity, out layout))
        {
            return true;
        }

        if (string.IsNullOrEmpty(device.Fingerprint) || connectedDevicesWithSameFingerprint != 1)
        {
            return false;
        }

        string[] candidateIds = Devices.Values
            .Where(preference => preference.Fingerprint == device.Fingerprint && LayoutMappings.ContainsKey(preference.Identity))
            .Select(preference => preference.Identity)
            .ToArray();

        Layout[] candidates = candidateIds.Select(identity => LayoutMappings[identity]).ToArray();
        if (candidates.Length == 0 || candidates.Select(candidate => candidate.Identifier).Distinct().Count() != 1)
        {
            return false;
        }

        DevicePreference? previous = candidateIds.Select(identity => Devices[identity]).FirstOrDefault();
        DevicePreference current = TouchDevice(device, previous?.CustomName);
        current.CustomName = previous?.CustomName;
        layout = candidates[0];
        LayoutMappings[device.Identity] = layout;
        learnedIdentity = true;
        return true;
    }

    public bool IsIgnored(
        KeyboardDevice device,
        int connectedDevicesWithSameFingerprint,
        out bool learnedIdentity)
    {
        learnedIdentity = false;
        TouchDevice(device);
        if (IgnoredDevices.Contains(device.Identity))
        {
            return true;
        }

        string[] candidates = Devices.Values
            .Where(preference => preference.Fingerprint == device.Fingerprint && IgnoredDevices.Contains(preference.Identity))
            .Select(preference => preference.Identity)
            .ToArray();
        if (string.IsNullOrEmpty(device.Fingerprint) || connectedDevicesWithSameFingerprint != 1 ||
            candidates.Length != 1 ||
            Devices.Values.Any(preference =>
                preference.Fingerprint == device.Fingerprint && LayoutMappings.ContainsKey(preference.Identity)))
        {
            return false;
        }

        DevicePreference previous = Devices[candidates[0]];
        DevicePreference current = TouchDevice(device, previous.CustomName);
        current.CustomName = previous.CustomName;
        IgnoredDevices.Add(device.Identity);
        learnedIdentity = true;
        return true;
    }

    public void SetLayout(KeyboardDevice device, Layout layout, string? customName = null)
    {
        TouchDevice(device, customName);
        IgnoredDevices.Remove(device.Identity);
        LayoutMappings[device.Identity] = layout;
    }

    public void Ignore(KeyboardDevice device, string? customName = null)
    {
        TouchDevice(device, customName);
        LayoutMappings.Remove(device.Identity);
        IgnoredDevices.Add(device.Identity);
    }

    public void UpdatePreference(string identity, string? customName, Layout? layout, bool ignored)
    {
        if (!Devices.TryGetValue(identity, out DevicePreference? preference))
        {
            throw new InvalidOperationException("El dispositivo ya no existe en las preferencias.");
        }

        preference.CustomName = NormalizeCustomName(customName, preference.DetectedName);
        if (ignored)
        {
            IgnoredDevices.Add(identity);
            LayoutMappings.Remove(identity);
        }
        else
        {
            IgnoredDevices.Remove(identity);
            if (layout is null)
            {
                LayoutMappings.Remove(identity);
            }
            else
            {
                LayoutMappings[identity] = layout;
            }
        }
    }

    public void Forget(string identity)
    {
        Devices.Remove(identity);
        LayoutMappings.Remove(identity);
        IgnoredDevices.Remove(identity);
    }

    public void MergeFrom(Configuration imported, bool replace)
    {
        if (replace)
        {
            Devices.Clear();
            LayoutMappings.Clear();
            IgnoredDevices.Clear();
        }

        foreach ((string identity, DevicePreference preference) in imported.Devices)
        {
            Devices[identity] = preference.Clone();
        }

        foreach ((string identity, Layout layout) in imported.LayoutMappings)
        {
            LayoutMappings[identity] = layout;
            IgnoredDevices.Remove(identity);
        }

        foreach (string identity in imported.IgnoredDevices)
        {
            IgnoredDevices.Add(identity);
            LayoutMappings.Remove(identity);
        }
    }

    public void Save(string? configurationFile = null) =>
        SaveToPath(configurationFile ?? GetConfigFilePath());

    public void Export(string path) => SaveToPath(path);

    public string CreateBackup()
    {
        Directory.CreateDirectory(GetExportDirectory());
        string path = Path.Combine(GetExportDirectory(), $"RightKeyboard-respaldo-{DateTime.Now:yyyy-MM-dd-HHmmss}.json");
        Export(path);
        return path;
    }

    public void Clear(string? configurationFile = null)
    {
        Devices.Clear();
        LayoutMappings.Clear();
        IgnoredDevices.Clear();
        Save(configurationFile);
    }

    public static string GetConfigFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "preferences.json");

    public static string GetLegacyConfigFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "config.txt");

    public static string GetExportDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "exports");

    private void SaveToPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        StoredConfigurationV3 stored = new()
        {
            Devices = Devices.Values
                .OrderBy(preference => preference.DisplayName)
                .ThenBy(preference => preference.Identity)
                .Select(preference => StoredDevice.From(preference))
                .ToList(),
            Mappings = LayoutMappings
                .OrderBy(pair => pair.Key)
                .Select(pair => StoredMappingV3.From(pair.Key, pair.Value))
                .ToList(),
            IgnoredDeviceIds = IgnoredDevices.Order(StringComparer.OrdinalIgnoreCase).ToList()
        };

        string temporaryPath = fullPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(stored, JsonOptions));
        File.Move(temporaryPath, fullPath, true);
    }

    private static Configuration LoadJson(string path, IReadOnlyList<Layout> layouts)
    {
        string json = File.ReadAllText(path);
        using JsonDocument document = JsonDocument.Parse(json);
        int version = document.RootElement.TryGetProperty("version", out JsonElement versionElement)
            ? versionElement.GetInt32()
            : 2;
        return version >= 3 ? LoadV3(json, layouts) : LoadV2(json, layouts);
    }

    private static Configuration LoadV3(string json, IReadOnlyList<Layout> layouts)
    {
        StoredConfigurationV3 stored = JsonSerializer.Deserialize<StoredConfigurationV3>(json, JsonOptions)
            ?? new StoredConfigurationV3();
        Configuration configuration = new();
        foreach (StoredDevice device in stored.Devices.Where(device => !string.IsNullOrWhiteSpace(device.Identity)))
        {
            configuration.Devices[device.Identity] = device.ToPreference();
        }

        foreach (StoredMappingV3 mapping in stored.Mappings)
        {
            if (TryReadLayout(mapping.Layout, mapping.LanguageName, mapping.LayoutName, layouts, out Layout? layout) &&
                configuration.Devices.ContainsKey(mapping.Identity))
            {
                configuration.LayoutMappings[mapping.Identity] = layout!;
            }
        }

        foreach (string identity in stored.IgnoredDeviceIds.Where(configuration.Devices.ContainsKey))
        {
            configuration.IgnoredDevices.Add(identity);
            configuration.LayoutMappings.Remove(identity);
        }

        return configuration;
    }

    private static Configuration LoadV2(string json, IReadOnlyList<Layout> layouts)
    {
        StoredConfigurationV2 stored = JsonSerializer.Deserialize<StoredConfigurationV2>(json, JsonOptions)
            ?? new StoredConfigurationV2();
        Configuration configuration = new();

        foreach (StoredMappingV2 mapping in stored.Mappings)
        {
            if (!TryReadLayout(mapping.Layout, null, null, layouts, out Layout? layout) || string.IsNullOrWhiteSpace(mapping.Identity))
            {
                continue;
            }

            configuration.Devices[mapping.Identity] = new DevicePreference
            {
                Identity = mapping.Identity,
                Fingerprint = mapping.Fingerprint ?? string.Empty,
                DetectedName = mapping.DisplayName ?? "Teclado",
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            configuration.LayoutMappings[mapping.Identity] = layout!;
        }

        foreach (StoredIgnoredDeviceV2 ignored in stored.IgnoredDevices.Where(item => !string.IsNullOrWhiteSpace(item.Identity)))
        {
            configuration.Devices[ignored.Identity] = new DevicePreference
            {
                Identity = ignored.Identity,
                Fingerprint = ignored.Fingerprint ?? string.Empty,
                DetectedName = ignored.DisplayName ?? "Dispositivo ignorado",
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            configuration.IgnoredDevices.Add(ignored.Identity);
        }

        return configuration;
    }

    private static Configuration LoadLegacy(
        KeyboardDevicesCollection devices,
        IReadOnlyList<Layout> layouts,
        string path)
    {
        Configuration configuration = new();
        foreach (string line in File.ReadLines(path))
        {
            int separator = line.LastIndexOf('=');
            if (separator <= 0 || separator == line.Length - 1 ||
                !TryReadLayout(line[(separator + 1)..], null, null, layouts, out Layout? layout))
            {
                continue;
            }

            string storedIdentity = line[..separator];
            string identity = storedIdentity.StartsWith("container:", StringComparison.OrdinalIgnoreCase) ||
                              storedIdentity.StartsWith("device:", StringComparison.OrdinalIgnoreCase) ||
                              storedIdentity.StartsWith("instance:", StringComparison.OrdinalIgnoreCase)
                ? storedIdentity
                : devices.GetIdentityFromLegacyName(storedIdentity);
            KeyboardDevice? connected = devices
                .Where(device => device.Identity == identity)
                .Select(device => (KeyboardDevice?)device)
                .FirstOrDefault();
            configuration.Devices[identity] = new DevicePreference
            {
                Identity = identity,
                Fingerprint = connected?.Fingerprint ?? string.Empty,
                DetectedName = connected?.DisplayName ?? "Teclado migrado",
                TechnicalId = connected?.TechnicalId ?? string.Empty,
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            configuration.LayoutMappings[identity] = layout!;
        }

        return configuration;
    }

    private static bool TryReadLayout(
        string? identifierValue,
        string? languageName,
        string? layoutName,
        IReadOnlyList<Layout> layouts,
        out Layout? layout)
    {
        layout = null;
        if (ulong.TryParse(identifierValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong identifier))
        {
            layout = layouts.FirstOrDefault(candidate => candidate.Identifier == new nint(unchecked((long)identifier)));
        }

        layout ??= layouts.FirstOrDefault(candidate =>
            string.Equals(candidate.LanguageName, languageName, StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(candidate.LayoutName, layoutName, StringComparison.CurrentCultureIgnoreCase));
        return layout is not null;
    }

    private static string BuildSuggestedName(string detectedName, string technicalId) =>
        string.Equals(detectedName, "Teclado sin nombre", StringComparison.CurrentCultureIgnoreCase)
            ? $"Teclado {technicalId.Split(' ').LastOrDefault()}".Trim()
            : detectedName;

    private static string? NormalizeCustomName(string? customName, string detectedName)
    {
        string? normalized = string.IsNullOrWhiteSpace(customName) ? null : customName.Trim();
        return string.Equals(normalized, detectedName, StringComparison.CurrentCultureIgnoreCase) ? null : normalized;
    }

    private sealed class StoredConfigurationV3
    {
        public int Version { get; set; } = CurrentSchemaVersion;
        public List<StoredDevice> Devices { get; set; } = [];
        public List<StoredMappingV3> Mappings { get; set; } = [];
        public List<string> IgnoredDeviceIds { get; set; } = [];
    }

    private sealed class StoredDevice
    {
        public string Identity { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
        public string DetectedName { get; set; } = string.Empty;
        public string? CustomName { get; set; }
        public string TechnicalId { get; set; } = string.Empty;
        public DateTimeOffset LastSeenUtc { get; set; }

        public static StoredDevice From(DevicePreference preference) => new()
        {
            Identity = preference.Identity,
            Fingerprint = preference.Fingerprint,
            DetectedName = preference.DetectedName,
            CustomName = preference.CustomName,
            TechnicalId = preference.TechnicalId,
            LastSeenUtc = preference.LastSeenUtc
        };

        public DevicePreference ToPreference() => new()
        {
            Identity = Identity,
            Fingerprint = Fingerprint,
            DetectedName = DetectedName,
            CustomName = CustomName,
            TechnicalId = TechnicalId,
            LastSeenUtc = LastSeenUtc
        };
    }

    private sealed class StoredMappingV3
    {
        public string Identity { get; set; } = string.Empty;
        public string Layout { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string LayoutName { get; set; } = string.Empty;

        public static StoredMappingV3 From(string identity, Layout layout) => new()
        {
            Identity = identity,
            Layout = unchecked((ulong)layout.Identifier.ToInt64()).ToString("X16"),
            LanguageName = layout.LanguageName,
            LayoutName = layout.LayoutName
        };
    }

    private sealed class StoredConfigurationV2
    {
        public List<StoredMappingV2> Mappings { get; set; } = [];
        public List<StoredIgnoredDeviceV2> IgnoredDevices { get; set; } = [];
    }

    private sealed class StoredMappingV2
    {
        public string Identity { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
        public string? DisplayName { get; set; }
        public string Layout { get; set; } = string.Empty;
    }

    private sealed class StoredIgnoredDeviceV2
    {
        public string Identity { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
        public string? DisplayName { get; set; }
    }
}

public sealed class DevicePreference
{
    public required string Identity { get; init; }
    public string Fingerprint { get; set; } = string.Empty;
    public string DetectedName { get; set; } = string.Empty;
    public string? CustomName { get; set; }
    public string TechnicalId { get; set; } = string.Empty;
    public DateTimeOffset LastSeenUtc { get; set; }
    public string DisplayName => string.IsNullOrWhiteSpace(CustomName) ? DetectedName : CustomName;

    public DevicePreference Clone() => new()
    {
        Identity = Identity,
        Fingerprint = Fingerprint,
        DetectedName = DetectedName,
        CustomName = CustomName,
        TechnicalId = TechnicalId,
        LastSeenUtc = LastSeenUtc
    };
}
