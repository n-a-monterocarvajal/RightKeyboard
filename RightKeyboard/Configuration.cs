using System.Globalization;
using System.Text.Json;

namespace RightKeyboard;

public sealed class Configuration
{
    private const int CurrentSchemaVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public Dictionary<string, DeviceLayoutPreference> LayoutMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, IgnoredDevicePreference> IgnoredDevices { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static Configuration LoadConfiguration(
        KeyboardDevicesCollection devices,
        string? configurationFile = null,
        IEnumerable<Layout>? availableLayouts = null)
    {
        string path = configurationFile ?? GetConfigFilePath();
        IReadOnlyDictionary<nint, Layout> layouts = (availableLayouts ?? Layout.EnumerateLayouts())
            .ToDictionary(layout => layout.Identifier);

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

    public bool TryGetLayout(
        KeyboardDevice device,
        int connectedDevicesWithSameFingerprint,
        out Layout? layout,
        out bool learnedIdentity)
    {
        learnedIdentity = false;
        if (LayoutMappings.TryGetValue(device.Identity, out DeviceLayoutPreference? exact))
        {
            layout = exact.Layout;
            return true;
        }

        if (string.IsNullOrEmpty(device.Fingerprint) || connectedDevicesWithSameFingerprint != 1)
        {
            layout = null;
            return false;
        }

        DeviceLayoutPreference[] candidates = LayoutMappings.Values
            .Where(preference => preference.Fingerprint == device.Fingerprint)
            .ToArray();

        if (candidates.Length == 0 || candidates.Select(candidate => candidate.Layout.Identifier).Distinct().Count() != 1)
        {
            layout = null;
            return false;
        }

        layout = candidates[0].Layout;
        LayoutMappings[device.Identity] = new DeviceLayoutPreference(
            device.Identity,
            device.Fingerprint,
            device.DisplayName,
            layout);
        learnedIdentity = true;
        return true;
    }

    public bool IsIgnored(
        KeyboardDevice device,
        int connectedDevicesWithSameFingerprint,
        out bool learnedIdentity)
    {
        learnedIdentity = false;
        if (IgnoredDevices.ContainsKey(device.Identity))
        {
            return true;
        }

        IgnoredDevicePreference[] candidates = IgnoredDevices.Values
            .Where(preference => preference.Fingerprint == device.Fingerprint)
            .ToArray();
        if (string.IsNullOrEmpty(device.Fingerprint) || connectedDevicesWithSameFingerprint != 1 ||
            candidates.Length != 1 ||
            LayoutMappings.Values.Any(preference => preference.Fingerprint == device.Fingerprint))
        {
            return false;
        }

        IgnoredDevices[device.Identity] = new IgnoredDevicePreference(
            device.Identity,
            device.Fingerprint,
            device.DisplayName);
        learnedIdentity = true;
        return true;
    }

    public void SetLayout(KeyboardDevice device, Layout layout)
    {
        IgnoredDevices.Remove(device.Identity);
        LayoutMappings[device.Identity] = new DeviceLayoutPreference(
            device.Identity,
            device.Fingerprint,
            device.DisplayName,
            layout);
    }

    public void Ignore(KeyboardDevice device)
    {
        LayoutMappings.Remove(device.Identity);
        IgnoredDevices[device.Identity] = new IgnoredDevicePreference(
            device.Identity,
            device.Fingerprint,
            device.DisplayName);
    }

    public void Save(string? configurationFile = null)
    {
        string fullPath = Path.GetFullPath(configurationFile ?? GetConfigFilePath());
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        StoredConfiguration stored = new()
        {
            Version = CurrentSchemaVersion,
            Mappings = LayoutMappings.Values
                .OrderBy(preference => preference.DisplayName)
                .ThenBy(preference => preference.Identity)
                .Select(preference => new StoredMapping
                {
                    Identity = preference.Identity,
                    Fingerprint = preference.Fingerprint,
                    DisplayName = preference.DisplayName,
                    Layout = unchecked((ulong)preference.Layout.Identifier.ToInt64()).ToString("X16")
                })
                .ToList(),
            IgnoredDevices = IgnoredDevices.Values
                .OrderBy(preference => preference.DisplayName)
                .ThenBy(preference => preference.Identity)
                .Select(preference => new StoredIgnoredDevice
                {
                    Identity = preference.Identity,
                    Fingerprint = preference.Fingerprint,
                    DisplayName = preference.DisplayName
                })
                .ToList()
        };

        string temporaryPath = fullPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(stored, JsonOptions));
        File.Move(temporaryPath, fullPath, true);
    }

    public void Clear(string? configurationFile = null)
    {
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

    private static Configuration LoadJson(string path, IReadOnlyDictionary<nint, Layout> layouts)
    {
        StoredConfiguration stored = JsonSerializer.Deserialize<StoredConfiguration>(File.ReadAllText(path), JsonOptions)
            ?? new StoredConfiguration();
        Configuration configuration = new();

        foreach (StoredMapping mapping in stored.Mappings)
        {
            if (TryReadLayout(mapping.Layout, layouts, out Layout? layout) && !string.IsNullOrWhiteSpace(mapping.Identity))
            {
                configuration.LayoutMappings[mapping.Identity] = new DeviceLayoutPreference(
                    mapping.Identity,
                    mapping.Fingerprint ?? string.Empty,
                    mapping.DisplayName ?? "Teclado",
                    layout!);
            }
        }

        foreach (StoredIgnoredDevice ignored in stored.IgnoredDevices)
        {
            if (!string.IsNullOrWhiteSpace(ignored.Identity))
            {
                configuration.IgnoredDevices[ignored.Identity] = new IgnoredDevicePreference(
                    ignored.Identity,
                    ignored.Fingerprint ?? string.Empty,
                    ignored.DisplayName ?? "Dispositivo ignorado");
            }
        }

        return configuration;
    }

    private static Configuration LoadLegacy(
        KeyboardDevicesCollection devices,
        IReadOnlyDictionary<nint, Layout> layouts,
        string path)
    {
        Configuration configuration = new();
        foreach (string line in File.ReadLines(path))
        {
            int separator = line.LastIndexOf('=');
            if (separator <= 0 || separator == line.Length - 1 ||
                !TryReadLayout(line[(separator + 1)..], layouts, out Layout? layout))
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
            configuration.LayoutMappings[identity] = new DeviceLayoutPreference(
                identity,
                connected?.Fingerprint ?? string.Empty,
                connected?.DisplayName ?? "Teclado migrado",
                layout!);
        }

        return configuration;
    }

    private static bool TryReadLayout(
        string? value,
        IReadOnlyDictionary<nint, Layout> layouts,
        out Layout? layout)
    {
        layout = null;
        if (!ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong identifier))
        {
            return false;
        }

        return layouts.TryGetValue(new nint(unchecked((long)identifier)), out layout);
    }

    private sealed class StoredConfiguration
    {
        public int Version { get; set; } = CurrentSchemaVersion;
        public List<StoredMapping> Mappings { get; set; } = [];
        public List<StoredIgnoredDevice> IgnoredDevices { get; set; } = [];
    }

    private sealed class StoredMapping
    {
        public string Identity { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
        public string? DisplayName { get; set; }
        public string Layout { get; set; } = string.Empty;
    }

    private sealed class StoredIgnoredDevice
    {
        public string Identity { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
        public string? DisplayName { get; set; }
    }
}

public sealed record DeviceLayoutPreference(
    string Identity,
    string Fingerprint,
    string DisplayName,
    Layout Layout);

public sealed record IgnoredDevicePreference(
    string Identity,
    string Fingerprint,
    string DisplayName);
