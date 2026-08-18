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
    public void CheckBox_HeredaElRadioDelControlSinImponerUnoPropio()
    {
        // El template enlaza el rectángulo del glifo con CheckBox.CornerRadius: propagar el
        // radio del control es exactamente lo que produce el glifo redondeado nativo.
        Assert.That(
            SettingsPanelVisualContract.CheckBoxGlyphCornerRadius,
            Is.EqualTo(SettingsPanelVisualContract.ControlCornerRadius));
    }

    [Test]
    public void Radios_DeRespaldoCoincidenConLosValoresNativosDeFluent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SettingsPanelVisualContract.ControlCornerRadius, Is.EqualTo(4));
            Assert.That(SettingsPanelVisualContract.OverlayCornerRadius, Is.EqualTo(8));
            Assert.That(
                SettingsPanelVisualContract.ControlCornerRadius,
                Is.LessThan(SettingsPanelVisualContract.OverlayCornerRadius),
                "Una superficie se redondea más que un control.");
        });
    }

    [Test]
    public void Recargar_UsaUnGlifoProporcionadoDentroDeUnObjetivoCompacto()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SettingsPanelVisualContract.ReloadIconSize, Is.EqualTo(16));
            Assert.That(
                SettingsPanelVisualContract.ReloadIconSize,
                Is.EqualTo(SettingsPanelVisualContract.ReloadButtonSize / 2));
        });
    }

    [Test]
    public void FilaDeDispositivo_SeparaElIndicadorDeSeleccionDelTexto()
    {
        Assert.That(
            SettingsPanelVisualContract.DeviceRowContentLeftPadding,
            Is.GreaterThan(SettingsPanelVisualContract.DeviceRowContentPadding),
            "Sin margen izquierdo adicional, la barra de selección queda pegada al nombre.");
    }

    [Test]
    public void Subtitulo_NombraDispositivosYExplicaLaAsignacionDeDistribucion()
    {
        Assert.That(
            SettingsPanelVisualContract.SettingsSubtitle,
            Is.EqualTo(
                "Administra los dispositivos detectados. " +
                "Asigna a los teclados la distribución deseada."));
    }

    [Test]
    public void AyudaDelEditor_PrecisaEnQueListaPermanecenLasIdentidades()
    {
        Assert.That(
            SettingsPanelVisualContract.EditorSectionDescription,
            Does.EndWith("permanecen visibles en la lista de dispositivos detectados."));
    }

    [Test]
    public void AvisoDeEdicion_NoDependeDeUnSeparadorTipografico()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                SettingsPanelVisualContract.AliasEditingHint,
                Is.EqualTo("La identificación automática se reanudará al dejar de escribir."));
            Assert.That(
                SettingsPanelVisualContract.AliasEditingHint,
                Does.Not.StartWith("·"),
                "El separador se sustituyó por un glifo de información.");
            Assert.That(SettingsPanelVisualContract.InformationGlyph, Is.Not.Empty);
        });
    }
}
