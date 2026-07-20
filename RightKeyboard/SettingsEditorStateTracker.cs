namespace RightKeyboard;

internal readonly record struct SettingsEditorState(
    string Alias,
    long? LayoutIdentifier,
    bool Ignored);

internal enum UnsavedChangesDecision
{
    Discard,
    KeepEditing
}

internal sealed class SettingsEditorStateTracker
{
    private SettingsEditorState persistedState;
    private SettingsEditorState editorState;
    private bool hasState;

    internal bool IsDirty => hasState && editorState != persistedState;

    internal void Load(SettingsEditorState state)
    {
        persistedState = state;
        editorState = state;
        hasState = true;
    }

    internal void Update(SettingsEditorState state, bool applyingEditorState)
    {
        if (!hasState || applyingEditorState)
        {
            return;
        }

        editorState = state;
    }

    internal void Clear() => hasState = false;

    internal bool CanLeave(UnsavedChangesDecision decision) =>
        !IsDirty || decision == UnsavedChangesDecision.Discard;
}
