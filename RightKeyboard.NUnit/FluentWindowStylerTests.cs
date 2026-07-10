using System.Drawing;
using System.Windows.Forms;
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

    [TestCase(0, 1, false, true)]
    [TestCase(1, 0, true, false)]
    [TestCase(1, 1, false, false)]
    public void ResolveDarkMode_PriorizaElTemaVisibleDelSistema(
        int systemUsesLightTheme,
        int appsUseLightTheme,
        bool fallback,
        bool expected)
    {
        Assert.That(
            FluentTheme.ResolveDarkMode(systemUsesLightTheme, appsUseLightTheme, fallback),
            Is.EqualTo(expected));
    }

    [Test]
    public void ResolveDarkMode_UsaPreferenciaDeAplicacionesSiFaltaLaDelSistema()
    {
        Assert.That(FluentTheme.ResolveDarkMode(null, 0, fallback: false), Is.True);
    }

    [Test]
    public void ResolveDarkMode_UsaFallbackCuandoNoHayPreferencias()
    {
        Assert.That(FluentTheme.ResolveDarkMode(null, null, fallback: true), Is.True);
    }

    [Test]
    public void ApplyPalette_TemaClaroMantieneTextoLegibleEnCadaSuperficie()
    {
        FluentPalette palette = FluentPalette.Create(darkMode: false);
        using Form form = new();
        using Panel panel = new();
        using Label label = new() { Text = "Texto" };
        using TextBox textBox = new() { Text = "Campo" };
        using Button button = new() { Text = "Acción" };
        panel.Controls.AddRange([label, textBox, button]);
        form.Controls.Add(panel);

        FluentTheme.ApplyPalette(form, palette);

        Assert.Multiple(() =>
        {
            Assert.That(form.BackColor, Is.EqualTo(palette.Window));
            Assert.That(label.ForeColor, Is.EqualTo(palette.Text));
            Assert.That(textBox.BackColor, Is.EqualTo(palette.Field));
            Assert.That(textBox.ForeColor, Is.EqualTo(palette.Text));
            Assert.That(button.BackColor, Is.EqualTo(palette.Surface));
            Assert.That(button.ForeColor, Is.EqualTo(palette.Text));
            Assert.That(ContrastRatio(label.ForeColor, palette.Window), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(ContrastRatio(textBox.ForeColor, textBox.BackColor), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(ContrastRatio(button.ForeColor, button.BackColor), Is.GreaterThanOrEqualTo(4.5));
        });
    }

    private static double ContrastRatio(Color foreground, Color background)
    {
        static double Luminance(Color color)
        {
            static double Linear(byte component)
            {
                double value = component / 255d;
                return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));
        }

        double lighter = Math.Max(Luminance(foreground), Luminance(background));
        double darker = Math.Min(Luminance(foreground), Luminance(background));
        return (lighter + 0.05) / (darker + 0.05);
    }

}
