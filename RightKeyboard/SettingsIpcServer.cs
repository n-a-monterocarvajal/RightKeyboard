using System.IO.Pipes;
using System.Diagnostics;
using System.Text.Json;

namespace RightKeyboard;

internal sealed class SettingsIpcServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Configuration configuration;
    private readonly KeyboardDevicesCollection devices;
    private readonly SynchronizationContext uiContext;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task serverTask;
    private readonly DiagnosticLogger diagnostics;
    private long activitySequence;
    private string? activeDeviceIdentity;

    internal SettingsIpcServer(
        Configuration configuration,
        KeyboardDevicesCollection devices,
        SynchronizationContext uiContext,
        DiagnosticLogger diagnostics)
    {
        this.configuration = configuration;
        this.devices = devices;
        this.uiContext = uiContext;
        this.diagnostics = diagnostics;
        serverTask = Task.Run(ListenAsync);
    }

    internal void NotifyDeviceInput(string identity)
    {
        activeDeviceIdentity = identity;
        Interlocked.Increment(ref activitySequence);
    }

    private async Task ListenAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeServerStream pipe = new(
                    SettingsIpcProtocol.PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellation.Token);
                await HandleConnectionAsync(pipe, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Una solicitud dañada no debe terminar el proceso residente.
            }
        }
    }

    private async Task HandleConnectionAsync(Stream pipe, CancellationToken token)
    {
        SettingsResponse response;
        try
        {
            using StreamReader reader = new(pipe, leaveOpen: true);
            string? json = await reader.ReadLineAsync(token);
            SettingsRequest? request = json is null
                ? null
                : JsonSerializer.Deserialize<SettingsRequest>(json, JsonOptions);
            response = request is null
                ? new SettingsResponse(false, "La solicitud está vacía.", null)
                : await InvokeOnUiThreadAsync(() => Execute(request));
        }
        catch (Exception error)
        {
            response = new SettingsResponse(false, error.Message, null);
        }

        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions).AsMemory(), token);
    }

    private SettingsResponse Execute(SettingsRequest request)
    {
        if (request.Version != SettingsIpcProtocol.Version)
        {
            return new SettingsResponse(false, "La versión del protocolo no es compatible.", null);
        }

        switch (request.Action)
        {
            case SettingsIpcProtocol.ActivityAction:
                return new SettingsResponse(
                    true,
                    null,
                    null,
                    new SettingsActivity(Interlocked.Read(ref activitySequence), activeDeviceIdentity));
            case SettingsIpcProtocol.DiagnosticsAction:
                return DiagnosticsResponse();
            case SettingsIpcProtocol.SetDiagnosticsAction:
                diagnostics.SetDetailedEnabled(request.DiagnosticsEnabled == true);
                return DiagnosticsResponse();
            case SettingsIpcProtocol.OpenDiagnosticsAction:
                Directory.CreateDirectory(diagnostics.DirectoryPath);
                Process.Start(new ProcessStartInfo("explorer.exe", diagnostics.DirectoryPath)
                {
                    UseShellExecute = true
                });
                return DiagnosticsResponse();
            case SettingsIpcProtocol.SnapshotAction:
                break;
            case SettingsIpcProtocol.SaveAction:
                RequireIdentity(request);
                Layout? layout = request.LayoutIdentifier is long id
                    ? Layout.EnumerateLayouts().FirstOrDefault(candidate => candidate.Identifier.ToInt64() == id)
                    : null;
                if (request.LayoutIdentifier is not null && layout is null)
                {
                    throw new InvalidOperationException("La distribución seleccionada ya no está disponible.");
                }
                configuration.UpdatePreference(request.Identity!, request.CustomName, layout, request.Ignored == true);
                configuration.Save();
                break;
            case SettingsIpcProtocol.ForgetAction:
                RequireIdentity(request);
                configuration.Forget(request.Identity!);
                configuration.Save();
                break;
            case SettingsIpcProtocol.ClearAction:
                configuration.Clear();
                break;
            default:
                return new SettingsResponse(false, "La acción solicitada no existe.", null);
        }

        return new SettingsResponse(true, null, CreateSnapshot());
    }

    private SettingsResponse DiagnosticsResponse() => new(
        true,
        null,
        null,
        Diagnostics: new SettingsDiagnostics(diagnostics.IsDetailedEnabled, diagnostics.DirectoryPath));

    private SettingsSnapshot CreateSnapshot()
    {
        // El residente mantiene este inventario mediante WM_DEVICECHANGE. Volver a
        // recorrer SetupAPI en cada apertura retrasaba innecesariamente ambas UI.
        KeyboardDevice[] visibleDevices = devices
            .Where(device => !DeviceClassifier.IsLikelySyntheticInputSource(device))
            .ToArray();
        HashSet<string> connected = visibleDevices.Select(device => device.Identity)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (KeyboardDevice device in visibleDevices)
        {
            configuration.TouchDevice(device);
        }

        SettingsDevice[] deviceRows = configuration.Devices.Values
            .OrderBy(preference => preference.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(preference => new SettingsDevice(
                preference.Identity,
                preference.DisplayName,
                preference.DetectedName,
                preference.TechnicalId,
                preference.LastSeenUtc,
                connected.Contains(preference.Identity),
                configuration.IgnoredDevices.Contains(preference.Identity),
                configuration.LayoutMappings.TryGetValue(preference.Identity, out Layout? layout)
                    ? layout.Identifier.ToInt64()
                    : null))
            .ToArray();
        SettingsLayout[] layouts = Layout.EnumerateLayouts()
            .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
            .Select(layout => new SettingsLayout(
                layout.Identifier.ToInt64(), layout.LanguageName, layout.LayoutName))
            .ToArray();
        return new SettingsSnapshot(SettingsIpcProtocol.Version, deviceRows, layouts);
    }

    private static void RequireIdentity(SettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Identity))
        {
            throw new InvalidOperationException("No se indicó un dispositivo.");
        }
    }

    private Task<T> InvokeOnUiThreadAsync<T>(Func<T> action)
    {
        TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        uiContext.Post(_ =>
        {
            try { completion.SetResult(action()); }
            catch (Exception error) { completion.SetException(error); }
        }, null);
        return completion.Task;
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try { serverTask.Wait(TimeSpan.FromSeconds(2)); }
        catch (AggregateException error) when (error.InnerExceptions.All(inner => inner is OperationCanceledException)) { }
        cancellation.Dispose();
    }
}
