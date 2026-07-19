namespace RightKeyboard;

internal static class SettingsIpcProtocol
{
    internal const string PipeName = "RightKeyboard.Settings.v2";
    internal const int Version = 2;
    internal const string SnapshotAction = "snapshot";
    internal const string SaveAction = "save";
    internal const string ForgetAction = "forget";
    internal const string ClearAction = "clear";
    internal const string ActivityAction = "activity";
    internal const string DiagnosticsAction = "diagnostics";
    internal const string SetDiagnosticsAction = "set-diagnostics";
    internal const string OpenDiagnosticsAction = "open-diagnostics";
    internal const string ExportAction = "export";
    internal const string ImportPreviewAction = "import-preview";
    internal const string ImportApplyAction = "import-apply";
    internal const string StartupAction = "startup";
    internal const string SetStartupAction = "set-startup";
    internal const string GroupAction = "group";
    internal const string UngroupAction = "ungroup";
}

internal sealed record SettingsRequest(
    int Version,
    string Action,
    string? Identity = null,
    string? CustomName = null,
    long? LayoutIdentifier = null,
    bool? Ignored = null,
    bool? DiagnosticsEnabled = null,
    string? FilePath = null,
    bool? Replace = null,
    bool? StartupEnabled = null,
    string? TargetIdentity = null);

internal sealed record SettingsResponse(
    bool Success,
    string? Error,
    SettingsSnapshot? Snapshot,
    SettingsActivity? Activity = null,
    SettingsDiagnostics? Diagnostics = null,
    SettingsImportPreview? ImportPreview = null,
    SettingsStartup? Startup = null);

internal sealed record SettingsImportPreview(int DeviceCount, IReadOnlyList<string> Warnings);

internal sealed record SettingsStartup(bool Enabled);

internal sealed record SettingsActivity(long Sequence, string? Identity);

internal sealed record SettingsDiagnostics(bool Enabled, string DirectoryPath);

internal sealed record SettingsSnapshot(
    int Version,
    IReadOnlyList<SettingsDevice> Devices,
    IReadOnlyList<SettingsDeviceGroup> Groups,
    IReadOnlyList<SettingsLayout> Layouts);

internal sealed record SettingsDevice(
    string Identity,
    string DisplayName,
    string DetectedName,
    string TechnicalId,
    DateTimeOffset LastSeenUtc,
    bool Connected,
    bool Ignored,
    long? LayoutIdentifier,
    string? GroupId);

internal sealed record SettingsDeviceGroup(
    string Id,
    string DisplayName,
    long? LayoutIdentifier,
    IReadOnlyList<string> MemberIdentities);

internal sealed record SettingsLayout(long Identifier, string LanguageName, string LayoutName)
{
    public string Name => $"{LanguageName} / {LayoutName}";

    public override string ToString() => Name;
}
