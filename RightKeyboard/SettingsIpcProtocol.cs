namespace RightKeyboard;

internal static class SettingsIpcProtocol
{
    internal const string PipeName = "RightKeyboard.Settings.v1";
    internal const int Version = 1;
    internal const string SnapshotAction = "snapshot";
    internal const string SaveAction = "save";
    internal const string ForgetAction = "forget";
    internal const string ClearAction = "clear";
    internal const string ActivityAction = "activity";
}

internal sealed record SettingsRequest(
    int Version,
    string Action,
    string? Identity = null,
    string? CustomName = null,
    long? LayoutIdentifier = null,
    bool? Ignored = null);

internal sealed record SettingsResponse(
    bool Success,
    string? Error,
    SettingsSnapshot? Snapshot,
    SettingsActivity? Activity = null);

internal sealed record SettingsActivity(long Sequence, string? Identity);

internal sealed record SettingsSnapshot(
    int Version,
    IReadOnlyList<SettingsDevice> Devices,
    IReadOnlyList<SettingsLayout> Layouts);

internal sealed record SettingsDevice(
    string Identity,
    string DisplayName,
    string DetectedName,
    string TechnicalId,
    DateTimeOffset LastSeenUtc,
    bool Connected,
    bool Ignored,
    long? LayoutIdentifier);

internal sealed record SettingsLayout(long Identifier, string LanguageName, string LayoutName)
{
    public string Name => $"{LanguageName} / {LayoutName}";

    public override string ToString() => Name;
}
