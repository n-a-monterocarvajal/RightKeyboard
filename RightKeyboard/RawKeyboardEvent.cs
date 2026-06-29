namespace RightKeyboard;

public readonly record struct RawKeyboardEvent(
    nint DeviceHandle,
    ushort VirtualKey,
    ushort MakeCode,
    ushort Flags,
    uint Message)
{
    private const ushort KeyBreak = 0x0001;
    private const ushort FakeKey = 0x00FF;

    public bool IsKeyDown => (Flags & KeyBreak) == 0;

    public bool CanStartMapping => IsKeyDown && VirtualKey != FakeKey && !IsModifier(VirtualKey);

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
