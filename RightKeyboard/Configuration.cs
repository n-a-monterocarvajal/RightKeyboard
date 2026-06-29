using System.Globalization;

namespace RightKeyboard;

public sealed class Configuration
{
    public Dictionary<string, Layout> LayoutMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static Configuration LoadConfiguration(
        KeyboardDevicesCollection devices,
        string? configurationFile = null)
    {
        Configuration configuration = new();
        string path = configurationFile ?? GetConfigFilePath();
        if (!File.Exists(path))
        {
            return configuration;
        }

        Dictionary<nint, Layout> layouts = Layout.EnumerateLayouts().ToDictionary(layout => layout.Identifier);
        foreach (string line in File.ReadLines(path))
        {
            int separator = line.LastIndexOf('=');
            if (separator <= 0 || separator == line.Length - 1)
            {
                continue;
            }

            string storedIdentity = line[..separator];
            string identity = storedIdentity.StartsWith("container:", StringComparison.OrdinalIgnoreCase) ||
                              storedIdentity.StartsWith("device:", StringComparison.OrdinalIgnoreCase)
                ? storedIdentity
                : devices.GetIdentityFromLegacyName(storedIdentity);

            if (!ulong.TryParse(line[(separator + 1)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong value))
            {
                continue;
            }

            nint layoutIdentifier = new(unchecked((long)value));
            if (layouts.TryGetValue(layoutIdentifier, out Layout? layout))
            {
                configuration.LayoutMappings[identity] = layout;
            }
        }

        return configuration;
    }

    public void Save(string? configurationFile = null)
    {
        string path = configurationFile ?? GetConfigFilePath();
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        string temporaryPath = fullPath + ".tmp";
        using (StreamWriter output = File.CreateText(temporaryPath))
        {
            foreach ((string identity, Layout layout) in LayoutMappings.OrderBy(pair => pair.Key))
            {
                ulong value = unchecked((ulong)layout.Identifier.ToInt64());
                output.WriteLine($"{identity}={value:X16}");
            }
        }

        File.Move(temporaryPath, fullPath, true);
    }

    public void Clear(string? configurationFile = null)
    {
        LayoutMappings.Clear();
        Save(configurationFile);
    }

    public static string GetConfigFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "config.txt");
}
