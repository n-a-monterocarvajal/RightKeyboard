using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class DeviceNamingTests
{
    private const string TechnicalId = "Dispositivo F7E55424";

    [TestCase("Dispositivo sin nombre")]
    [TestCase("Teclado sin nombre")]
    [TestCase("  teclado SIN nombre  ")]
    [TestCase("")]
    [TestCase("   ")]
    public void IsUnnamed_ReconoceElLiteralActualYElAnterior(string detectedName)
    {
        Assert.That(DeviceNaming.IsUnnamed(detectedName), Is.True);
    }

    [TestCase("Baseus Presenter")]
    [TestCase("Logitech MX Keys")]
    public void IsUnnamed_NoConfundeUnNombreUtil(string detectedName)
    {
        Assert.That(DeviceNaming.IsUnnamed(detectedName), Is.False);
    }

    [Test]
    public void GetIdentityLabel_SinNombreUtil_UsaElIdentificadorTecnico()
    {
        // Es lo único que distingue dos identidades del mismo aparato conectado
        // en puertos distintos, que es el caso que motiva la agrupación.
        Assert.That(
            DeviceNaming.GetIdentityLabel("Teclado sin nombre", TechnicalId),
            Is.EqualTo(TechnicalId));
    }

    [Test]
    public void GetIdentityLabel_ConNombreUtil_ConservaElNombreDetectado()
    {
        Assert.That(
            DeviceNaming.GetIdentityLabel("Baseus Presenter", TechnicalId),
            Is.EqualTo("Baseus Presenter"));
    }

    [Test]
    public void GetIdentityLabel_SinNombreNiIdentificador_CaeEnElNombreNeutro()
    {
        Assert.That(
            DeviceNaming.GetIdentityLabel(null, null),
            Is.EqualTo(DeviceNaming.UnnamedDevice));
    }

    [Test]
    public void GetSecondaryTechnicalId_CuandoLaEtiquetaYaEsElIdentificador_NoLoRepite()
    {
        Assert.That(
            DeviceNaming.GetSecondaryTechnicalId("Teclado sin nombre", TechnicalId),
            Is.Null);
    }

    [Test]
    public void GetSecondaryTechnicalId_ConNombreUtil_LoOfreceParaDistinguirDuplicados()
    {
        // Dos miembros del mismo modelo comparten nombre detectado; el
        // identificador es lo que los separa en la lista.
        Assert.That(
            DeviceNaming.GetSecondaryTechnicalId("Baseus Presenter", TechnicalId),
            Is.EqualTo(TechnicalId));
    }

    [Test]
    public void NombresNeutros_NoAtribuyenCategoriaDeTeclado()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DeviceNaming.UnnamedDevice, Does.Not.Contain("Teclado"));
            Assert.That(DeviceNaming.UnnamedGroup, Does.Not.Contain("Teclado"));
        });
    }
}
