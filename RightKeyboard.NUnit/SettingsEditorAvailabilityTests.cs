using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class SettingsEditorAvailabilityTests
{
    [Test]
    public void Create_DispositivoIgnorado_NoPuedeAgruparComoOrigen()
    {
        SettingsEditorAvailability availability = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.Device,
            ignored: true,
            hasSelectedGroupTarget: false);

        Assert.Multiple(() =>
        {
            Assert.That(availability.LayoutEnabled, Is.False);
            Assert.That(availability.GroupTargetEnabled, Is.False);
            Assert.That(availability.GroupButtonEnabled, Is.False);
        });
    }

    [Test]
    public void CanBeGroupTarget_DispositivoIgnorado_NoPuedeSerDestino()
    {
        Assert.That(
            SettingsEditorAvailability.CanBeGroupTarget(SettingsEditorRowKind.Device, ignored: true),
            Is.False);
    }

    [Test]
    public void Create_MarcarIgnorar_LimpiaElDestinoSeleccionadoQueDejaDeSerValido()
    {
        SettingsEditorAvailability availability = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.Device,
            ignored: true,
            hasSelectedGroupTarget: true);

        Assert.Multiple(() =>
        {
            Assert.That(availability.ClearInvalidGroupTargetSelection, Is.True);
            Assert.That(availability.GroupButtonEnabled, Is.False);
        });
    }

    [Test]
    public void Create_DesmarcarIgnorar_RecuperaSoloLasAccionesValidas()
    {
        SettingsEditorAvailability device = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.Device,
            ignored: false,
            hasSelectedGroupTarget: false);
        SettingsEditorAvailability member = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.GroupMember,
            ignored: false,
            hasSelectedGroupTarget: false);

        Assert.Multiple(() =>
        {
            Assert.That(device.LayoutEnabled, Is.True);
            Assert.That(device.GroupTargetEnabled, Is.True);
            Assert.That(device.GroupButtonEnabled, Is.False, "Falta seleccionar un destino.");
            Assert.That(member.LayoutEnabled, Is.False);
            Assert.That(member.GroupTargetEnabled, Is.False);
            Assert.That(member.UngroupEnabled, Is.True);
        });
    }

    [Test]
    public void Create_CargaProgramaticaDeshabilitada_NoLimpiaUnDestinoLogicamenteValido()
    {
        SettingsEditorAvailability availability = SettingsEditorAvailability.Create(
            editorEnabled: false,
            SettingsEditorRowKind.Device,
            ignored: false,
            hasSelectedGroupTarget: true);

        Assert.Multiple(() =>
        {
            Assert.That(availability.GroupTargetEnabled, Is.False);
            Assert.That(availability.GroupButtonEnabled, Is.False);
            Assert.That(availability.ClearInvalidGroupTargetSelection, Is.False);
        });
    }
}
