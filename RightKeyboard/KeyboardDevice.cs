namespace RightKeyboard;

public readonly record struct KeyboardDevice(
    string DevicePath,
    nint Handle,
    string Identity,
    string Fingerprint,
    string DisplayName,
    string TechnicalId,
    bool IsClearlyNonKeyboard,
    KeyboardDeviceCapabilities? Capabilities = null,
    string? Signature = null);

public sealed record KeyboardDeviceCapabilities(
    uint Type,
    uint SubType,
    uint KeyboardMode,
    uint NumberOfFunctionKeys,
    uint NumberOfIndicators,
    uint NumberOfKeysTotal);
