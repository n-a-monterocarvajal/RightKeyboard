using System.Runtime.InteropServices;
using NUnit.Framework;
using RightKeyboard.Win32;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class RawInputInteropTests
{
    [Test]
    public void RawInputHeader_HasTheWindowsNativeSize()
    {
        int expected = IntPtr.Size == 8 ? 24 : 16;

        Assert.That(Marshal.SizeOf<RAWINPUTHEADER>(), Is.EqualTo(expected));
    }

    [Test]
    public void KeyboardRawInput_FitsTheFixedBuffer()
    {
        int expected = IntPtr.Size == 8 ? 48 : 40;

        Assert.That(Marshal.SizeOf<RAWINPUT>(), Is.EqualTo(expected));
    }
}
