using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class SettingsPanelVisualContractTests
{
    [Test]
    public void Recargar_ConservaNombreAccesibleSinDependerDelContenidoVisible()
    {
        Assert.That(SettingsPanelVisualContract.ReloadAccessibleName, Is.EqualTo("Recargar"));
    }

    [Test]
    public void Recargar_ExplicaElEfectoEnLaListaDeDispositivos()
    {
        Assert.That(
            SettingsPanelVisualContract.ReloadToolTip,
            Is.EqualTo("Recarga la lista de dispositivos detectados."));
    }

    [Test]
    public void CheckBox_UsaUnRadioProporcionadoAlRestoDeControles()
    {
        Assert.That(
            SettingsPanelVisualContract.CheckBoxGlyphCornerRadius,
            Is.EqualTo(SettingsPanelVisualContract.ControlCornerRadius / 2));
    }
}
