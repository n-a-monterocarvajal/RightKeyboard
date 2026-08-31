using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class SettingsEditorAvailabilityTests
{
    [Test]
    public void Create_DispositivoIgnoradoYaGuardado_PuedeAgruparPeroNoElegirDistribucion()
    {
        SettingsEditorAvailability availability = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.Device,
            ignored: true,
            hasSelectedGroupTarget: true);

        Assert.Multiple(() =>
        {
            Assert.That(availability.LayoutEnabled, Is.False, "Un ignorado no tiene distribución.");
            Assert.That(availability.GroupTargetEnabled, Is.True);
            Assert.That(availability.GroupButtonEnabled, Is.True);
            Assert.That(availability.ClearInvalidGroupTargetSelection, Is.False);
        });
    }

    [Test]
    public void CanBeGroupTarget_DispositivoIgnorado_PuedeSerDestino()
    {
        Assert.That(
            SettingsEditorAvailability.CanBeGroupTarget(SettingsEditorRowKind.Device),
            Is.True);
    }

    [Test]
    public void Create_GrupoLogico_OfreceLaCasillaIgnorar()
    {
        SettingsEditorAvailability availability = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.Group,
            ignored: false,
            hasSelectedGroupTarget: false);

        Assert.Multiple(() =>
        {
            Assert.That(availability.ShowIgnored, Is.True);
            Assert.That(availability.IgnoredEnabled, Is.True);
        });
    }

    [Test]
    public void Create_CambioDeIgnorarSinGuardar_LimpiaElDestinoYEsperaAlGuardado()
    {
        // Los candidatos se listan según el estado guardado; con la casilla
        // recién marcada dejarían de corresponder al estado que se agruparía.
        SettingsEditorAvailability availability = SettingsEditorAvailability.Create(
            editorEnabled: true,
            SettingsEditorRowKind.Device,
            ignored: true,
            hasSelectedGroupTarget: true,
            ignoredChangePending: true);

        Assert.Multiple(() =>
        {
            Assert.That(availability.ClearInvalidGroupTargetSelection, Is.True);
            Assert.That(availability.GroupTargetEnabled, Is.False);
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

    [Test]
    public void IsGroupTargetCandidate_DosDispositivosSueltos_OfreceElOtroComoDestino()
    {
        // Regresión: al seleccionar un dispositivo suelto, otro dispositivo suelto debe
        // aparecer en el desplegable. Ambos tienen GroupId nulo y eso no significa que
        // compartan grupo.
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: true,
                candidateGroupId: null,
                candidateTargetIdentity: "Notebook",
                candidateIgnored: false,
                selectedGroupId: null,
                selectedTargetIdentity: "Escritorio2",
                selectedIgnored: false),
            Is.True);
    }

    [Test]
    public void IsGroupTargetCandidate_LaPropiaFilaSeleccionada_NoEsDestino()
    {
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: true,
                candidateGroupId: null,
                candidateTargetIdentity: "Escritorio2",
                candidateIgnored: false,
                selectedGroupId: null,
                selectedTargetIdentity: "Escritorio2",
                selectedIgnored: false),
            Is.False);
    }

    [Test]
    public void IsGroupTargetCandidate_CandidatoNoAgrupable_NoEsDestino()
    {
        // p. ej. un grupo o un miembro de grupo.
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: false,
                candidateGroupId: null,
                candidateTargetIdentity: "Control Baseus",
                candidateIgnored: false,
                selectedGroupId: null,
                selectedTargetIdentity: "Escritorio2",
                selectedIgnored: false),
            Is.False);
    }

    [Test]
    public void IsGroupTargetCandidate_OrigenGrupo_OfreceDispositivosSueltos()
    {
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: true,
                candidateGroupId: null,
                candidateTargetIdentity: "Notebook",
                candidateIgnored: false,
                selectedGroupId: "grupo-1",
                selectedTargetIdentity: "Escritorio2",
                selectedIgnored: false),
            Is.True);
    }

    [Test]
    public void IsGroupTargetCandidate_DosIgnorados_SeOfrecenEntreSi()
    {
        // El caso de uso de 1.6.1: el mismo dispositivo ignorado reaparece con
        // otra identidad al cambiar de puerto USB.
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: true,
                candidateGroupId: null,
                candidateTargetIdentity: "Control Baseus (puerto trasero)",
                candidateIgnored: true,
                selectedGroupId: null,
                selectedTargetIdentity: "Control Baseus",
                selectedIgnored: true),
            Is.True);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void IsGroupTargetCandidate_EstadosDistintos_NoEsDestino(bool candidateIgnored, bool selectedIgnored)
    {
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: true,
                candidateGroupId: null,
                candidateTargetIdentity: "Notebook",
                candidateIgnored,
                selectedGroupId: null,
                selectedTargetIdentity: "Escritorio2",
                selectedIgnored),
            Is.False);
    }

    [Test]
    public void IsGroupTargetCandidate_MismoGrupoReal_NoSeDuplicaComoDestino()
    {
        Assert.That(
            SettingsEditorAvailability.IsGroupTargetCandidate(
                candidateCanBeGroupTarget: true,
                candidateGroupId: "grupo-1",
                candidateTargetIdentity: "Notebook",
                candidateIgnored: false,
                selectedGroupId: "grupo-1",
                selectedTargetIdentity: "Escritorio2",
                selectedIgnored: false),
            Is.False);
    }
}
