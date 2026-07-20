namespace RightKeyboard;

internal enum SettingsEditorRowKind
{
    None,
    Device,
    Group,
    GroupMember
}

internal readonly record struct SettingsEditorAvailability(
    bool AliasEnabled,
    bool LayoutEnabled,
    bool IgnoredEnabled,
    bool SaveEnabled,
    bool ForgetEnabled,
    bool GroupTargetEnabled,
    bool GroupButtonEnabled,
    bool UngroupEnabled,
    bool ShowIgnored,
    bool ShowGrouping,
    bool ShowUngroup,
    bool ClearInvalidGroupTargetSelection)
{
    internal static SettingsEditorAvailability Create(
        bool editorEnabled,
        SettingsEditorRowKind rowKind,
        bool ignored,
        bool hasSelectedGroupTarget)
    {
        bool logicalPreference = editorEnabled && rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group;
        bool canGroup = CanGroupAsSource(rowKind, ignored);
        bool groupTargetEnabled = editorEnabled && canGroup;

        return new SettingsEditorAvailability(
            AliasEnabled: logicalPreference,
            LayoutEnabled: logicalPreference && !ignored,
            IgnoredEnabled: editorEnabled && rowKind == SettingsEditorRowKind.Device,
            SaveEnabled: logicalPreference,
            ForgetEnabled: editorEnabled && rowKind == SettingsEditorRowKind.Device,
            GroupTargetEnabled: groupTargetEnabled,
            GroupButtonEnabled: groupTargetEnabled && hasSelectedGroupTarget,
            UngroupEnabled: editorEnabled && rowKind == SettingsEditorRowKind.GroupMember,
            ShowIgnored: rowKind == SettingsEditorRowKind.Device,
            ShowGrouping: rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group,
            ShowUngroup: rowKind == SettingsEditorRowKind.GroupMember,
            ClearInvalidGroupTargetSelection: hasSelectedGroupTarget && !canGroup);
    }

    internal static bool CanGroupAsSource(SettingsEditorRowKind rowKind, bool ignored) =>
        rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group && !ignored;

    internal static bool CanBeGroupTarget(SettingsEditorRowKind rowKind, bool ignored) =>
        rowKind == SettingsEditorRowKind.Device && !ignored;
}
