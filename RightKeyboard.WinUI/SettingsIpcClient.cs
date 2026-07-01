using System.IO.Pipes;
using System.Text.Json;

namespace RightKeyboard.WinUI;

internal sealed class SettingsIpcClient
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

    private static async Task<SettingsSnapshot> SendAsync(SettingsRequest request)
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
        if (response is null || !response.Success || response.Snapshot is null)
        {
            throw new InvalidOperationException(response?.Error ?? "El núcleo de RightKeyboard no respondió.");
        }

        return response.Snapshot;
    }
}
