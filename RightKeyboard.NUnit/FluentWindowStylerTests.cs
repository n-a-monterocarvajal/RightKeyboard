using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class FluentWindowStylerTests
{
    [TestCase(10, 0, 19045, false)]
    [TestCase(10, 0, 22000, false)]
    [TestCase(10, 0, 22621, true)]
    [TestCase(10, 0, 26100, true)]
    public void CanUseSystemBackdrop_ExigeWindows11_22H2(
        int major,
        int minor,
        int build,
        bool expected)
    {
        Assert.That(
            FluentBackdropPolicy.CanUseSystemBackdrop(new Version(major, minor, build), highContrast: false),
            Is.EqualTo(expected));
    }

    [Test]
    public void CanUseSystemBackdrop_RespetaContrasteAlto()
    {
        Assert.That(
            FluentBackdropPolicy.CanUseSystemBackdrop(new Version(10, 0, 26100), highContrast: true),
            Is.False);
    }

    [TestCase(10, 0, 19045, false)]
    [TestCase(10, 0, 22000, true)]
    [TestCase(10, 0, 26100, true)]
    public void CanUseRoundedFrame_DegradaEnWindows10(int major, int minor, int build, bool expected)
    {
        Assert.That(FluentBackdropPolicy.CanUseRoundedFrame(new Version(major, minor, build)), Is.EqualTo(expected));
    }
}
