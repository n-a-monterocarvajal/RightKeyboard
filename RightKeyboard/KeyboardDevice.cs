namespace RightKeyboard;

public readonly record struct KeyboardDevice(
    string DevicePath,
    nint Handle,
    string Identity,
    string Fingerprint,
    string DisplayName,
    string TechnicalId,
    bool IsClearlyNonKeyboard);
