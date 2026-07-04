namespace RightKeyboard;

public readonly record struct RawKeyboardEvent(
    nint DeviceHandle,
    ushort VirtualKey,
    ushort MakeCode,
    ushort Flags,
    uint Message,
    uint ExtraInformation = 0)
{
    private const ushort KeyBreak = 0x0001;
    private const uint KeyDown = 0x0100;
    private const uint SystemKeyDown = 0x0104;

    public bool IsKeyDown => (Flags & KeyBreak) == 0 && Message is KeyDown or SystemKeyDown;

    public bool IsSystemKeyDown => Message == SystemKeyDown;

    public bool HasScanCode => MakeCode != 0;

    public bool HasExtraInformation => ExtraInformation != 0;

    public bool IsExtendedKey => (Flags & 0x0002) != 0;

    public bool CanStartMapping => IsKeyDown && IsUsableKey(VirtualKey) && !IsModifier(VirtualKey);

    private static bool IsUsableKey(ushort key) => key is not (
        0x00 or // Sin tecla virtual
        0xE5 or // VK_PROCESSKEY
        0xE7 or // VK_PACKET
        0xFF);  // Tecla falsa de Raw Input

    private static bool IsModifier(ushort key) => key is
        0x10 or // Shift
        0x11 or // Control
        0x12 or // Alt
        0x5B or // Windows izquierda
        0x5C or // Windows derecha
        0xA0 or 0xA1 or // Shift izquierdo/derecho
        0xA2 or 0xA3 or // Control izquierdo/derecho
        0xA4 or 0xA5;   // Alt izquierdo/derecho
}
