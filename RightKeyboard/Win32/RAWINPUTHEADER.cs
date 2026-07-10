using System.Runtime.InteropServices;

namespace RightKeyboard.Win32;

[StructLayout(LayoutKind.Sequential)]
internal struct RAWINPUTHEADER
{
    public uint Type;
    public uint Size;
    public nint Device;
    public nint WParam;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RAWKEYBOARD
{
    public ushort MakeCode;
    public ushort Flags;
    public ushort Reserved;
    public ushort VirtualKey;
    public uint Message;
    public uint ExtraInformation;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RAWINPUT
{
    public RAWINPUTHEADER Header;
    public RAWINPUTDATA Data;
}

// RAWINPUT contiene una unión cuyo miembro mayor (RAWMOUSE) ocupa 24 bytes.
[StructLayout(LayoutKind.Explicit, Size = 24)]
internal struct RAWINPUTDATA
{
    [FieldOffset(0)]
    public RAWKEYBOARD Keyboard;
}
