using System.IO.Pipes;
using System.Text.Json;

namespace RightKeyboard.WinUI;

public sealed class SettingsIpcClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal Task<SettingsSnapshot> GetSnapshotAsync() =>
        SendAsync(new SettingsRequest(SettingsIpcProtocol.Version, SettingsIpcProtocol.SnapshotAction));

    internal Task<SettingsSnapshot> SaveAsync(string identity, string? customName, long? layout, bool ignored) =>
        SendAsync(new SettingsRequest(SettingsIpcProtocol.Version, SettingsIpcProtocol.SaveAction, identity, customName, layout, ignored));

    internal Task<SettingsSnapshot> ForgetAsync(string identity) =>
        SendAsync(new SettingsRequest(SettingsIpcProtocol.Version, SettingsIpcProtocol.ForgetAction, identity));

    internal Task<SettingsSnapshot> ClearAsync() =>
        SendAsync(new SettingsRequest(SettingsIpcProtocol.Version, SettingsIpcProtocol.ClearAction));

    internal async Task<SettingsActivity> GetActivityAsync()
    {
        SettingsResponse response = await SendResponseAsync(
            new SettingsRequest(SettingsIpcProtocol.Version, SettingsIpcProtocol.ActivityAction));
        return response.Activity ?? new SettingsActivity(0, null);
    }

    internal async Task<SettingsDiagnostics> GetDiagnosticsAsync() =>
        (await SendResponseAsync(new SettingsRequest(
            SettingsIpcProtocol.Version, SettingsIpcProtocol.DiagnosticsAction))).Diagnostics
        ?? throw new InvalidOperationException("El núcleo no devolvió el estado del diagnóstico.");

    internal async Task<SettingsDiagnostics> SetDiagnosticsAsync(bool enabled) =>
        (await SendResponseAsync(new SettingsRequest(
            SettingsIpcProtocol.Version,
            SettingsIpcProtocol.SetDiagnosticsAction,
            DiagnosticsEnabled: enabled))).Diagnostics
        ?? throw new InvalidOperationException("El núcleo no devolvió el estado del diagnóstico.");

    internal async Task OpenDiagnosticsAsync() =>
        _ = await SendResponseAsync(new SettingsRequest(
            SettingsIpcProtocol.Version, SettingsIpcProtocol.OpenDiagnosticsAction));

    private static async Task<SettingsSnapshot> SendAsync(SettingsRequest request)
    {
        SettingsResponse response = await SendResponseAsync(request);
        return response.Snapshot ?? throw new InvalidOperationException("El núcleo no devolvió la configuración.");
    }

    private static async Task<SettingsResponse> SendResponseAsync(SettingsRequest request)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await using NamedPipeClientStream pipe = new(
            ".", SettingsIpcProtocol.PipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(timeout.Token);
        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions).AsMemory(), timeout.Token);
        using StreamReader reader = new(pipe, leaveOpen: true);
        string? json = await reader.ReadLineAsync(timeout.Token);
        SettingsResponse? response = json is null
            ? null
            : JsonSerializer.Deserialize<SettingsResponse>(json, JsonOptions);
        if (response is null || !response.Success)
        {
            throw new InvalidOperationException(response?.Error ?? "El núcleo de RightKeyboard no respondió.");
        }

        return response;
    }
}
