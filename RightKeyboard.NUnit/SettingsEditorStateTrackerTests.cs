using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class SettingsEditorStateTrackerTests
{
    private static readonly SettingsEditorState Original = new("Teclado oficina", 0x0000040A, false);

    [Test]
    public void Load_DejaElEditorLimpio()
    {
        SettingsEditorStateTracker tracker = CreateTracker();

        Assert.That(tracker.IsDirty, Is.False);
    }

    [Test]
    public void Update_DetectaCambioDeAlias()
    {
        SettingsEditorStateTracker tracker = CreateTracker();

        tracker.Update(Original with { Alias = "Teclado casa" }, applyingEditorState: false);

        Assert.That(tracker.IsDirty, Is.True);
    }

    [Test]
    public void Update_DetectaCambioDeDistribucion()
    {
        SettingsEditorStateTracker tracker = CreateTracker();

        tracker.Update(Original with { LayoutIdentifier = 0x00000409 }, applyingEditorState: false);

        Assert.That(tracker.IsDirty, Is.True);
    }

    [Test]
    public void Update_DetectaCambioDeEstadoIgnorado()
    {
        SettingsEditorStateTracker tracker = CreateTracker();

        tracker.Update(Original with { Ignored = true }, applyingEditorState: false);

        Assert.That(tracker.IsDirty, Is.True);
    }

    [Test]
    public void Update_QuedaLimpioAlRestaurarLosTresValoresOriginales()
    {
        SettingsEditorStateTracker tracker = CreateTracker();
        tracker.Update(new SettingsEditorState("Otro", null, true), applyingEditorState: false);

        tracker.Update(Original, applyingEditorState: false);

        Assert.That(tracker.IsDirty, Is.False);
    }

    [Test]
    public void Update_IgnoraLaCargaProgramatica()
    {
        SettingsEditorStateTracker tracker = CreateTracker();

        tracker.Update(new SettingsEditorState("Carga", null, true), applyingEditorState: true);

        Assert.That(tracker.IsDirty, Is.False);
    }

    [Test]
    public void CanLeave_PermiteDescartarLosCambios()
    {
        SettingsEditorStateTracker tracker = CreateDirtyTracker();

        Assert.That(tracker.CanLeave(UnsavedChangesDecision.Discard), Is.True);
    }

    [Test]
    public void CanLeave_CancelaCambioDeFilaOCierreCuandoSeConservaLaEdicion()
    {
        SettingsEditorStateTracker tracker = CreateDirtyTracker();

        Assert.That(tracker.CanLeave(UnsavedChangesDecision.KeepEditing), Is.False);
    }

    [Test]
    public void CanLeave_NoSolicitaDescartarCuandoElEditorEstaLimpio()
    {
        SettingsEditorStateTracker tracker = CreateTracker();

        Assert.That(tracker.CanLeave(UnsavedChangesDecision.KeepEditing), Is.True);
    }

    private static SettingsEditorStateTracker CreateTracker()
    {
        SettingsEditorStateTracker tracker = new();
        tracker.Load(Original);
        return tracker;
    }

    private static SettingsEditorStateTracker CreateDirtyTracker()
    {
        SettingsEditorStateTracker tracker = CreateTracker();
        tracker.Update(Original with { Alias = "Modificado" }, applyingEditorState: false);
        return tracker;
    }
}
