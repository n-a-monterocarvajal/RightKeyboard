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

    public static bool IsClearlyNonKeyboard(string displayName)
    {
        string normalized = displayName.Trim().ToLowerInvariant();
        return !KeyboardTerms.Any(normalized.Contains) && NonKeyboardTerms.Any(normalized.Contains);
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
