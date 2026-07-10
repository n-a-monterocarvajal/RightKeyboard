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
            Assert.That(presentation.Mode, Is.EqualTo(spanish.Name));
            Assert.That(presentation.GetAccessibleName("Teclado oficina"), Does.Contain($"Estado: {expectedState}"));
        });
    }

    [Test]
    public void Create_PriorizaIgnoradoYSinDistribucion()
    {
        DevicePresentation ignored = DevicePresentation.Create(connected: false, ignored: true, spanish);
        DevicePresentation unassigned = DevicePresentation.Create(connected: true, ignored: false, layout: null);

        Assert.Multiple(() =>
        {
            Assert.That(ignored.SecondaryText, Is.EqualTo("Desconectado · Ignorado"));
            Assert.That(unassigned.SecondaryText, Is.EqualTo("Conectado · Sin distribución"));
        });
    }

    [Test]
    public void GetListText_RecortaSoloElAliasYConservaElEstado()
    {
        DevicePresentation presentation = DevicePresentation.Create(connected: false, ignored: true, spanish);

        string text = presentation.GetListText("Teclado oficina con un alias demasiado largo", maximumNameLength: 20);

        Assert.That(text, Is.EqualTo("Teclado oficina con…\r\nDesconectado · Ignorado"));
    }
}
