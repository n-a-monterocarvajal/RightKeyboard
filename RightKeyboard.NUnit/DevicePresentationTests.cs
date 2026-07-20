using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class DevicePresentationTests
{
    private readonly Layout spanish = new(new nint(0x0000040A), "español (Chile)", "Latinoamericano");

    [TestCase(true, "Conectado")]
    [TestCase(false, "Desconectado")]
    public void Create_DescribeElEstadoDeConexion(bool connected, string expectedState)
    {
        DevicePresentation presentation = DevicePresentation.Create(connected, ignored: false, spanish);

        Assert.Multiple(() =>
        {
            Assert.That(presentation.State, Is.EqualTo(expectedState));
            Assert.That(presentation.LayoutName, Is.EqualTo(spanish.Name));
            Assert.That(presentation.GetAccessibleName("Teclado oficina"), Does.Contain(expectedState));
        });
    }

    [TestCase(true, false, null, "Conectado · Sin distribución")]
    [TestCase(false, false, null, "Desconectado · Sin distribución")]
    [TestCase(true, true, null, "Conectado · Ignorado")]
    [TestCase(false, true, null, "Desconectado · Ignorado")]
    [TestCase(true, false, "español (Chile) - Latinoamericano", "Conectado · español (Chile) - Latinoamericano")]
    [TestCase(false, true, "español (Chile) - Latinoamericano", "Desconectado · Ignorado · español (Chile) - Latinoamericano")]
    public void Create_CombinaConexionIgnoradoYDistribucionSinOcultarInformacion(
        bool connected,
        bool ignored,
        string? layoutName,
        string expected)
    {
        DevicePresentation presentation = DevicePresentation.Create(connected, ignored, layoutName);

        Assert.That(presentation.SecondaryText, Is.EqualTo(expected));
    }

    [Test]
    public void GetAccessibleName_AnunciaConexionIgnoradoYDistribucion()
    {
        DevicePresentation presentation = DevicePresentation.Create(connected: true, ignored: true, spanish);

        Assert.That(
            presentation.GetAccessibleName("Teclado oficina"),
            Is.EqualTo($"Teclado oficina. Conectado. Ignorado. {spanish.Name}."));
    }

    [Test]
    public void GetListText_RecortaSoloElAliasYConservaElEstado()
    {
        DevicePresentation presentation = DevicePresentation.Create(connected: false, ignored: true, spanish);

        string text = presentation.GetListText("Teclado oficina con un alias demasiado largo", maximumNameLength: 20);

        Assert.That(text, Is.EqualTo($"Teclado oficina con…\r\nDesconectado · Ignorado · {spanish.Name}"));
    }
}
