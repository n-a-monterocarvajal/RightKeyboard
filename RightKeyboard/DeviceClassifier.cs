namespace RightKeyboard;

public static class DeviceClassifier
{
    private static readonly string[] KeyboardTerms = ["keyboard", "teclado", "keypad"];
    private static readonly string[] NonKeyboardTerms =
    [
        "mouse",
        "ratón",
        "raton",
        "trackball",
        "touchpad",
        "trackpad",
        "pointing device",
        "dispositivo señalador",
        "mx master",
        "mx anywhere"
    ];

    public static bool IsClearlyNonKeyboard(string displayName) =>
        DescribeNonKeyboardMatch(displayName) is not null;

    // Devuelve el término de NonKeyboardTerms que provocó la clasificación como
    // no-teclado, o null si el dispositivo no se clasifica así. Sirve para explicar
    // falsos positivos en el diagnóstico; el término proviene de un vocabulario fijo,
    // por lo que es seguro registrarlo (no expone el nombre crudo del dispositivo).
    public static string? DescribeNonKeyboardMatch(string displayName)
    {
        string normalized = displayName.Trim().ToLowerInvariant();
        return KeyboardTerms.Any(normalized.Contains)
            ? null
            : Array.Find(NonKeyboardTerms, normalized.Contains);
    }

    public static bool IsLikelySyntheticInputSource(KeyboardDevice device)
    {
        KeyboardDeviceCapabilities? capabilities = device.Capabilities;
        if (capabilities is null || !string.IsNullOrWhiteSpace(device.Fingerprint))
        {
            return false;
        }

        bool emptyCapabilities = capabilities.Type == 0 &&
                                 capabilities.SubType == 0 &&
                                 capabilities.KeyboardMode == 0 &&
                                 capabilities.NumberOfFunctionKeys == 0 &&
                                 capabilities.NumberOfIndicators == 0 &&
                                 capabilities.NumberOfKeysTotal == 0;
        bool recognizedEnumerator = device.DevicePath.Contains("HID#", StringComparison.OrdinalIgnoreCase) ||
                                    device.DevicePath.Contains("ACPI#", StringComparison.OrdinalIgnoreCase) ||
                                    device.DevicePath.Contains("ROOT#", StringComparison.OrdinalIgnoreCase);
        return emptyCapabilities && !recognizedEnumerator;
    }
}
