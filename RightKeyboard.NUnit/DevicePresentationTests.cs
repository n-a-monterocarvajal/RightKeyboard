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

    [TestCase(true, false, true, 0)]
    [TestCase(true, false, false, 1)]
    [TestCase(true, true, true, 2)]
    [TestCase(false, false, true, 3)]
    [TestCase(false, false, false, 4)]
    [TestCase(false, true, true, 5)]
    public void GetSortRank_OrdenaLasSeisCombinacionesLogicas(
        bool connected,
        bool ignored,
        bool configured,
        int expectedRank)
    {
        Assert.That(DevicePresentation.GetSortRank(connected, ignored, configured), Is.EqualTo(expectedRank));
    }

    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    public void CreateGroup_ConsideraConectadoAlGrupoSiAlgunMiembroLoEsta(
        bool first,
        bool second,
        bool expectedConnected)
    {
        DevicePresentation presentation = DevicePresentation.CreateGroup([first, second], spanish);

        Assert.Multiple(() =>
        {
            Assert.That(presentation.Connected, Is.EqualTo(expectedConnected));
            Assert.That(presentation.LayoutName, Is.EqualTo(spanish.Name));
        });
    }

    [Test]
    public void OrdenPorRangoYNombre_ResuelveEmpatesPorNombre()
    {
        (string Name, DevicePresentation Presentation)[] rows =
        [
            ("Zulu", DevicePresentation.Create(true, ignored: false, spanish)),
            ("Alfa", DevicePresentation.Create(true, ignored: false, spanish)),
            ("Beta", DevicePresentation.Create(false, ignored: false, layoutName: null))
        ];

        string[] ordered = rows
            .OrderBy(row => row.Presentation.SortRank)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .Select(row => row.Name)
            .ToArray();

        Assert.That(ordered, Is.EqualTo(new[] { "Alfa", "Zulu", "Beta" }));
    }
}
