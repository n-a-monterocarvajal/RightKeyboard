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
}
