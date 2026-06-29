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
    public RAWKEYBOARD Keyboard;
}
