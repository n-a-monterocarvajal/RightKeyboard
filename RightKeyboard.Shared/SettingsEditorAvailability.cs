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
    /// <param name="ignored">Estado de la casilla «Ignorar», incluidos los cambios sin guardar.</param>
    /// <param name="ignoredChangePending">
    /// La casilla «Ignorar» difiere del estado guardado. Los candidatos a agrupar
    /// se listan según ese estado guardado, así que agrupar espera a que el
    /// cambio se guarde en vez de emparejar estados que ya no coinciden.
    /// </param>
    internal static SettingsEditorAvailability Create(
        bool editorEnabled,
        SettingsEditorRowKind rowKind,
        bool ignored,
        bool hasSelectedGroupTarget,
        bool ignoredChangePending = false)
    {
        bool logicalPreference = editorEnabled && rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group;
        bool canGroup = CanGroupAsSource(rowKind) && !ignoredChangePending;
        bool groupTargetEnabled = editorEnabled && canGroup;

        return new SettingsEditorAvailability(
            AliasEnabled: logicalPreference,
            LayoutEnabled: logicalPreference && !ignored,
            IgnoredEnabled: logicalPreference,
            SaveEnabled: logicalPreference,
            ForgetEnabled: editorEnabled && rowKind == SettingsEditorRowKind.Device,
            GroupTargetEnabled: groupTargetEnabled,
            GroupButtonEnabled: groupTargetEnabled && hasSelectedGroupTarget,
            UngroupEnabled: editorEnabled && rowKind == SettingsEditorRowKind.GroupMember,
            ShowIgnored: rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group,
            ShowGrouping: rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group,
            ShowUngroup: rowKind == SettingsEditorRowKind.GroupMember,
            ClearInvalidGroupTargetSelection: hasSelectedGroupTarget && !canGroup);
    }

    // Un dispositivo ignorado sí puede agruparse desde 1.6.1: es la vía manual
    // para declarar que dos identidades del mismo dispositivo ignorado —por
    // ejemplo, el mismo aparato en dos puertos USB— son el mismo aparato.
    internal static bool CanGroupAsSource(SettingsEditorRowKind rowKind) =>
        rowKind is SettingsEditorRowKind.Device or SettingsEditorRowKind.Group;

    internal static bool CanBeGroupTarget(SettingsEditorRowKind rowKind) =>
        rowKind == SettingsEditorRowKind.Device;

    /// <summary>
    /// Un grupo lógico tiene un solo estado efectivo, de modo que origen y
    /// destino deben coincidir en «ignorado»: mezclarlos dejaría al grupo sin
    /// una respuesta única a «¿debe ignorarse este dispositivo?».
    /// </summary>
    internal static bool IsGroupTargetCandidate(
        bool candidateCanBeGroupTarget,
        string? candidateGroupId,
        string candidateTargetIdentity,
        bool candidateIgnored,
        string? selectedGroupId,
        string selectedTargetIdentity,
        bool selectedIgnored) =>
        candidateCanBeGroupTarget &&
        candidateIgnored == selectedIgnored &&
        !IsSameLogicalGroup(candidateGroupId, selectedGroupId) &&
        !string.Equals(candidateTargetIdentity, selectedTargetIdentity, StringComparison.OrdinalIgnoreCase);

    private static bool IsSameLogicalGroup(string? left, string? right) =>
        left is not null && right is not null &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
