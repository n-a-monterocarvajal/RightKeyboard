using System.Globalization;
using System.Text.Json;

namespace RightKeyboard;

public sealed class Configuration
{
    public const int CurrentSchemaVersion = 3;
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
            return LoadJson(path, layouts, warnings: null);
        }

        if (configurationFile is null && File.Exists(GetLegacyConfigFilePath()))
        {
            Configuration migrated = LoadLegacy(devices, layouts, GetLegacyConfigFilePath());
            migrated.Save(path);
            return migrated;
        }

        return new Configuration();
    }

    public static ConfigurationImportResult LoadImport(
        string path,
        IEnumerable<Layout>? availableLayouts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Layout[] layouts = (availableLayouts ?? Layout.EnumerateLayouts()).ToArray();
        List<string> warnings = [];
        Configuration configuration = LoadJson(path, layouts, warnings);
        return new ConfigurationImportResult(configuration, warnings);
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
        ArgumentNullException.ThrowIfNull(imported);
        imported.ValidateState();

        if (replace)
        {
            Devices.Clear();
            LayoutMappings.Clear();
            IgnoredDevices.Clear();
        }

        foreach ((string identity, DevicePreference preference) in imported.Devices)
        {
            Devices[identity] = preference.Clone();
            LayoutMappings.Remove(identity);
            IgnoredDevices.Remove(identity);
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

    public string ApplyImport(
        Configuration imported,
        bool replace,
        string? configurationFile = null,
        string? backupDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(imported);
        Configuration candidate = Clone();
        candidate.MergeFrom(imported, replace);

        string backupPath = CreateBackup(backupDirectory);
        candidate.Save(configurationFile);
        ReplaceState(candidate);
        return backupPath;
    }

    public string CreateBackup(string? backupDirectory = null)
    {
        string directory = backupDirectory ?? GetExportDirectory();
        Directory.CreateDirectory(directory);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss-fff", CultureInfo.InvariantCulture);
        string path = Path.Combine(directory, $"RightKeyboard-respaldo-{timestamp}.json");
        for (int suffix = 2; File.Exists(path); suffix++)
        {
            path = Path.Combine(directory, $"RightKeyboard-respaldo-{timestamp}-{suffix}.json");
        }

        Export(path);
        return path;
    }

    public void Clear(string? configurationFile = null)
    {
        Configuration empty = new();
        empty.Save(configurationFile);
        ReplaceState(empty);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ValidateState();
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

        string temporaryPath = fullPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(stored, JsonOptions));
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static Configuration LoadJson(
        string path,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        string json = File.ReadAllText(path);
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw InvalidData("La raíz debe ser un objeto JSON.");
        }

        ValidateNoDuplicateProperties(document.RootElement, "$");

        JsonElement? versionElement = null;
        foreach (JsonProperty property in document.RootElement.EnumerateObject()
                     .Where(property => string.Equals(property.Name, "version", StringComparison.OrdinalIgnoreCase)))
        {
            if (versionElement is not null)
            {
                throw InvalidData("La propiedad 'version' está duplicada.");
            }

            versionElement = property.Value;
        }

        if (versionElement is null &&
            document.RootElement.EnumerateObject().Any(property =>
                string.Equals(property.Name, "devices", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "ignoredDeviceIds", StringComparison.OrdinalIgnoreCase)))
        {
            throw InvalidData("Falta la propiedad 'version' para un documento con estructura de esquema 3.");
        }

        int version = versionElement is JsonElement value ? ReadVersion(value) : 2;
        return version switch
        {
            2 => LoadV2(json, layouts, warnings),
            CurrentSchemaVersion => LoadV3(json, layouts, warnings),
            > CurrentSchemaVersion => throw InvalidData(
                $"El esquema {version} fue creado por una versión más reciente de RightKeyboard. " +
                $"Esta versión admite hasta el esquema {CurrentSchemaVersion}."),
            _ => throw InvalidData($"El esquema {version} no es compatible.")
        };
    }

    private static Configuration LoadV3(
        string json,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        StoredConfigurationV3 stored = JsonSerializer.Deserialize<StoredConfigurationV3>(json, JsonOptions)
            ?? throw InvalidData("El documento JSON no contiene preferencias.");
        stored.Devices ??= [];
        stored.Mappings ??= [];
        stored.IgnoredDeviceIds ??= [];
        Configuration configuration = new();
        HashSet<string> deviceIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (StoredDevice? device in stored.Devices)
        {
            if (device is null)
            {
                throw InvalidData("La colección 'devices' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(device.Identity, "devices");
            if (!deviceIds.Add(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' está duplicado.");
            }

            configuration.Devices[identity] = device.ToPreference(identity);
        }

        HashSet<string> mappingIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (StoredMappingV3? mapping in stored.Mappings)
        {
            if (mapping is null)
            {
                throw InvalidData("La colección 'mappings' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(mapping.Identity, "mappings");
            if (!mappingIds.Add(identity))
            {
                throw InvalidData($"La asociación de '{identity}' está duplicada.");
            }

            if (!configuration.Devices.ContainsKey(identity))
            {
                throw InvalidData($"La asociación de '{identity}' no corresponde a ningún dispositivo.");
            }

            if (TryReadLayout(mapping.Layout, mapping.LanguageName, mapping.LayoutName, layouts, out Layout? layout))
            {
                configuration.LayoutMappings[identity] = layout!;
            }
            else
            {
                warnings?.Add($"No se encontró la distribución de '{identity}'; el dispositivo se importará sin asociación.");
            }
        }

        HashSet<string> ignoredIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (string storedIdentity in stored.IgnoredDeviceIds)
        {
            string identity = RequireIdentity(storedIdentity, "ignoredDeviceIds");
            if (!ignoredIds.Add(identity))
            {
                throw InvalidData($"El dispositivo ignorado '{identity}' está duplicado.");
            }

            if (!configuration.Devices.ContainsKey(identity))
            {
                throw InvalidData($"El dispositivo ignorado '{identity}' no existe en devices.");
            }

            if (configuration.LayoutMappings.ContainsKey(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' no puede tener distribución y estar ignorado a la vez.");
            }

            configuration.IgnoredDevices.Add(identity);
        }

        return configuration;
    }

    private static Configuration LoadV2(
        string json,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        StoredConfigurationV2 stored = JsonSerializer.Deserialize<StoredConfigurationV2>(json, JsonOptions)
            ?? throw InvalidData("El documento JSON no contiene preferencias.");
        stored.Mappings ??= [];
        stored.IgnoredDevices ??= [];
        Configuration configuration = new();
        HashSet<string> identities = new(StringComparer.OrdinalIgnoreCase);

        foreach (StoredMappingV2? mapping in stored.Mappings)
        {
            if (mapping is null)
            {
                throw InvalidData("La colección 'mappings' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(mapping.Identity, "mappings");
            if (!identities.Add(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' está duplicado en el esquema 2.");
            }

            configuration.Devices[identity] = new DevicePreference
            {
                Identity = identity,
                Fingerprint = mapping.Fingerprint ?? string.Empty,
                DetectedName = mapping.DisplayName ?? "Teclado",
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            if (TryReadLayout(mapping.Layout, null, null, layouts, out Layout? layout))
            {
                configuration.LayoutMappings[identity] = layout!;
            }
            else
            {
                warnings?.Add($"No se encontró la distribución de '{identity}'; el dispositivo se importará sin asociación.");
            }
        }

        foreach (StoredIgnoredDeviceV2? ignored in stored.IgnoredDevices)
        {
            if (ignored is null)
            {
                throw InvalidData("La colección 'ignoredDevices' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(ignored.Identity, "ignoredDevices");
            if (!identities.Add(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' está duplicado en el esquema 2.");
            }

            configuration.Devices[identity] = new DevicePreference
            {
                Identity = identity,
                Fingerprint = ignored.Fingerprint ?? string.Empty,
                DetectedName = ignored.DisplayName ?? "Dispositivo ignorado",
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            configuration.IgnoredDevices.Add(identity);
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

    private Configuration Clone()
    {
        Configuration clone = new();
        clone.ReplaceState(this);
        return clone;
    }

    private void ReplaceState(Configuration source)
    {
        Devices.Clear();
        LayoutMappings.Clear();
        IgnoredDevices.Clear();

        foreach ((string identity, DevicePreference preference) in source.Devices)
        {
            Devices[identity] = preference.Clone();
        }

        foreach ((string identity, Layout layout) in source.LayoutMappings)
        {
            LayoutMappings[identity] = layout;
        }

        IgnoredDevices.UnionWith(source.IgnoredDevices);
    }

    private void ValidateState()
    {
        foreach ((string identity, DevicePreference preference) in Devices)
        {
            RequireIdentity(identity, "devices");
            if (!string.Equals(identity, preference.Identity, StringComparison.OrdinalIgnoreCase))
            {
                throw InvalidData($"La clave '{identity}' no coincide con la identidad del dispositivo.");
            }
        }

        foreach (string identity in LayoutMappings.Keys)
        {
            if (!Devices.ContainsKey(identity))
            {
                throw InvalidData($"La asociación de '{identity}' no corresponde a ningún dispositivo.");
            }

            if (IgnoredDevices.Contains(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' no puede tener distribución y estar ignorado a la vez.");
            }
        }

        foreach (string identity in IgnoredDevices)
        {
            if (!Devices.ContainsKey(identity))
            {
                throw InvalidData($"El dispositivo ignorado '{identity}' no existe en devices.");
            }
        }
    }

    private static int ReadVersion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out int version))
        {
            throw InvalidData("La propiedad 'version' debe ser un número entero.");
        }

        return version;
    }

    private static void ValidateNoDuplicateProperties(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw InvalidData($"La propiedad '{property.Name}' está duplicada en {path}.");
                }

                ValidateNoDuplicateProperties(property.Value, $"{path}.{property.Name}");
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                ValidateNoDuplicateProperties(item, $"{path}[{index++}]");
            }
        }
    }

    private static string RequireIdentity(string? value, string collection)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw InvalidData($"La colección '{collection}' contiene una identidad vacía.");
        }

        string identity = value.Trim();
        if (!string.Equals(identity, value, StringComparison.Ordinal))
        {
            throw InvalidData($"La identidad '{value}' contiene espacios al principio o al final.");
        }

        return identity;
    }

    private static InvalidDataException InvalidData(string message) =>
        new($"preferences.json no es válido: {message}");

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

        public DevicePreference ToPreference(string identity)
        {
            string detectedName = string.IsNullOrWhiteSpace(DetectedName) ? "Teclado" : DetectedName.Trim();
            return new DevicePreference
            {
                Identity = identity,
                Fingerprint = Fingerprint ?? string.Empty,
                DetectedName = detectedName,
                CustomName = NormalizeCustomName(CustomName, detectedName),
                TechnicalId = TechnicalId ?? string.Empty,
                LastSeenUtc = LastSeenUtc
            };
        }
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

public sealed record ConfigurationImportResult(
    Configuration Configuration,
    IReadOnlyList<string> Warnings);
