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

    /// <summary>
    /// Decide si una fila candidata debe ofrecerse como destino de agrupación para la
    /// fila seleccionada. Un candidato válido es un dispositivo agrupable que no es la
    /// propia fila seleccionada ni un miembro de su mismo grupo lógico.
    /// </summary>
    internal static bool IsGroupTargetCandidate(
        bool candidateCanBeGroupTarget,
        string? candidateGroupId,
        string candidateTargetIdentity,
        string? selectedGroupId,
        string selectedTargetIdentity) =>
        candidateCanBeGroupTarget &&
        !IsSameLogicalGroup(candidateGroupId, selectedGroupId) &&
        !string.Equals(candidateTargetIdentity, selectedTargetIdentity, StringComparison.OrdinalIgnoreCase);

    // Dos filas sin grupo (GroupId nulo) no comparten grupo: «sin grupo» no es «el mismo
    // grupo». Tratar dos nulos como iguales vaciaba el desplegable de agrupación al
    // seleccionar un dispositivo suelto, porque todos los candidatos válidos también
    // tienen GroupId nulo.
    private static bool IsSameLogicalGroup(string? left, string? right) =>
        left is not null && right is not null &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
