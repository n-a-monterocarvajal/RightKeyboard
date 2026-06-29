using System.Runtime.InteropServices;

namespace RightKeyboard.Win32;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RAWINPUTDEVICE
{
    private readonly ushort usagePage;
    private readonly ushort usage;
    private readonly uint flags;
    private readonly nint target;

    public RAWINPUTDEVICE(ushort usagePage, ushort usage, uint flags, nint target)
    {
        this.usagePage = usagePage;
        this.usage = usage;
        this.flags = flags;
        this.target = target;
    }
}
