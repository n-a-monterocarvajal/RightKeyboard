using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class SharedContractsTests
{
    [Test]
    public void ContratosDeAmbosProcesos_ResidenEnBibliotecaIndependiente()
    {
        Type[] contracts =
        [
            typeof(SettingsRequest),
            typeof(SettingsSnapshot),
            typeof(VersionPresentation),
            typeof(DevicePresentation),
            typeof(SettingsEditorStateTracker),
            typeof(SettingsEditorAvailability),
            typeof(SettingsPanelVisualContract),
            typeof(FrontendExitCodes),
            typeof(DiagnosticsAvailability)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(
                contracts.Select(type => type.Assembly).Distinct().Single().GetName().Name,
                Is.EqualTo("RightKeyboard.Shared"));
            Assert.That(typeof(SettingsRequest).Assembly, Is.Not.EqualTo(typeof(TrayApplicationContext).Assembly));
        });
    }

    [Test]
    public void VersionCompartida_CierraEnUnoSeisCero()
    {
        Assert.That(VersionPresentation.Current, Is.EqualTo("1.6.0"));
    }
}
