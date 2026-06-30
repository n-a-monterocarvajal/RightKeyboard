using System.Drawing;
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

    [TestCase(false)]
    [TestCase(true)]
    public void Palette_UsaColoresOpacosParaEvitarAcumulacionDeRepintado(bool darkMode)
    {
        FluentPalette palette = FluentPalette.Create(darkMode);
        Color[] colors =
        [
            palette.Window,
            palette.Surface,
            palette.Card,
            palette.Field,
            palette.Text,
            palette.SecondaryText,
            palette.Border,
            palette.Hover,
            palette.Selected,
            palette.Primary,
            palette.PrimaryText
        ];

        Assert.That(colors, Is.All.Matches<Color>(color => color.A == byte.MaxValue));
    }

    [Test]
    public void Palette_CambiaEntreTemaClaroYOscuro()
    {
        FluentPalette light = FluentPalette.Create(darkMode: false);
        FluentPalette dark = FluentPalette.Create(darkMode: true);

        Assert.Multiple(() =>
        {
            Assert.That(light.Window, Is.Not.EqualTo(dark.Window));
            Assert.That(light.Text, Is.Not.EqualTo(dark.Text));
            Assert.That(light.Hover, Is.Not.EqualTo(dark.Hover));
        });
    }

    [TestCase(0, false, true)]
    [TestCase(1, true, false)]
    [TestCase(1, false, false)]
    public void ResolveDarkMode_InterpretaLaPreferenciaDeAplicaciones(
        int appsUseLightTheme,
        bool fallback,
        bool expected)
    {
        Assert.That(
            FluentTheme.ResolveDarkMode(appsUseLightTheme, fallback),
            Is.EqualTo(expected));
    }

    [Test]
    public void ResolveDarkMode_UsaFallbackCuandoNoHayPreferencia()
    {
        Assert.That(FluentTheme.ResolveDarkMode(null, fallback: true), Is.True);
    }
}
