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
    private readonly DiagnosticLogger? diagnostics;
    private long activitySequence;
    private string? activeDeviceIdentity;

    internal SettingsIpcServer(
        Configuration configuration,
        KeyboardDevicesCollection devices,
        SynchronizationContext uiContext,
        DiagnosticLogger? diagnostics)
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
                if (diagnostics is null)
                {
                    return new SettingsResponse(false, "El diagnóstico no está disponible en esta compilación.", null);
                }

                return DiagnosticsResponse();
            case SettingsIpcProtocol.SetDiagnosticsAction:
                if (diagnostics is null)
                {
                    return new SettingsResponse(false, "El diagnóstico no está disponible en esta compilación.", null);
                }

                diagnostics.SetDetailedEnabled(request.DiagnosticsEnabled == true);
                return DiagnosticsResponse();
            case SettingsIpcProtocol.OpenDiagnosticsAction:
                if (diagnostics is null)
                {
                    return new SettingsResponse(false, "El diagnóstico no está disponible en esta compilación.", null);
                }

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
                Layout? layout = ResolveLayout(request);
                SavePreference(request, layout);
                configuration.Save();
                break;
            case SettingsIpcProtocol.ForgetAction:
                RequireIdentity(request);
                string? forgottenSignature = TrackedSignature(request.Identity!);
                configuration.Forget(request.Identity!);
                LogSignatureRemoval(forgottenSignature, "dispositivo_olvidado");
                configuration.Save();
                break;
            case SettingsIpcProtocol.GroupAction:
                RequireIdentity(request);
                string targetIdentity = RequireTargetIdentity(request);
                configuration.GroupDevices(
                    request.Identity!,
                    targetIdentity,
                    request.CustomName,
                    ResolveLayout(request),
                    overridePreference: true);
                configuration.Save();
                break;
            case SettingsIpcProtocol.UngroupAction:
                RequireIdentity(request);
                configuration.Ungroup(request.Identity!);
                configuration.Save();
                break;
            case SettingsIpcProtocol.ClearAction:
                configuration.Clear();
                break;
            case SettingsIpcProtocol.ExportAction:
                configuration.Export(RequireFilePath(request));
                return new SettingsResponse(true, null, null);
            case SettingsIpcProtocol.ImportPreviewAction:
                ConfigurationImportResult preview = Configuration.LoadImport(RequireFilePath(request));
                return new SettingsResponse(
                    true,
                    null,
                    null,
                    ImportPreview: new SettingsImportPreview(preview.Configuration.Devices.Count, preview.Warnings));
            case SettingsIpcProtocol.ImportApplyAction:
                ConfigurationImportResult import = Configuration.LoadImport(RequireFilePath(request));
                configuration.ApplyImport(import.Configuration, request.Replace == true);
                break;
            case SettingsIpcProtocol.StartupAction:
                return new SettingsResponse(true, null, null, Startup: new SettingsStartup(StartupManager.IsEnabled));
            case SettingsIpcProtocol.SetStartupAction:
                StartupManager.SetEnabled(request.StartupEnabled == true);
                return new SettingsResponse(true, null, null, Startup: new SettingsStartup(StartupManager.IsEnabled));
            default:
                return new SettingsResponse(false, "La acción solicitada no existe.", null);
        }

        return new SettingsResponse(true, null, CreateSnapshot());
    }

    private void SavePreference(SettingsRequest request, Layout? layout)
    {
        bool ignored = request.Ignored == true;
        KeyboardDevice? connectedDevice = devices
            .Where(device => string.Equals(device.Identity, request.Identity, StringComparison.OrdinalIgnoreCase))
            .Select(device => (KeyboardDevice?)device)
            .FirstOrDefault();
        string? trackedSignature = TrackedSignature(request.Identity!);

        bool signatureRegistered = configuration.UpdatePreference(
            request.Identity!, request.CustomName, layout, ignored, connectedDevice);

        if (ignored)
        {
            if (signatureRegistered)
            {
                diagnostics?.Write("firma_registrada", connectedDevice, new
                {
                    signature = connectedDevice?.Signature,
                    origin = "configuracion"
                });
            }
            else
            {
                diagnostics?.Write("firma_no_registrada", connectedDevice, new
                {
                    reason = connectedDevice is not KeyboardDevice device
                        ? "dispositivo_no_conectado"
                        : !string.IsNullOrEmpty(device.Fingerprint) ? "huella_presente" : "sin_vid_pid",
                    origin = "configuracion"
                });
            }
        }
        else
        {
            LogSignatureRemoval(trackedSignature, "preferencia_reactivada");
        }
    }

    /// <summary>Firma de la preferencia solo si sigue registrada como ignorada.</summary>
    private string? TrackedSignature(string identity) =>
        configuration.Devices.TryGetValue(identity, out DevicePreference? preference) &&
        preference.Signature is string signature &&
        configuration.IgnoredSignatures.Contains(signature)
            ? signature
            : null;

    private void LogSignatureRemoval(string? signature, string reason)
    {
        if (signature is not null && !configuration.IgnoredSignatures.Contains(signature))
        {
            diagnostics?.Write("firma_retirada", details: new { signature, reason });
        }
    }

    private SettingsResponse DiagnosticsResponse() => new(
        true,
        null,
        null,
        Diagnostics: diagnostics is null
            ? new SettingsDiagnostics(false, string.Empty)
            : new SettingsDiagnostics(diagnostics.IsDetailedEnabled, diagnostics.DirectoryPath));

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
                configuration.GetGroup(preference.Identity)?.DisplayName ?? preference.DisplayName,
                preference.DetectedName,
                preference.TechnicalId,
                preference.LastSeenUtc,
                connected.Contains(preference.Identity),
                configuration.IgnoredDevices.Contains(preference.Identity),
                configuration.TryGetEffectiveLayout(preference.Identity, out Layout? layout)
                    ? layout!.Identifier.ToInt64()
                    : null,
                configuration.GetGroup(preference.Identity)?.Id))
            .ToArray();
        SettingsDeviceGroup[] groups = configuration.DeviceGroups.Values
            .OrderBy(group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => new SettingsDeviceGroup(
                group.Id,
                group.DisplayName,
                group.Layout?.Identifier.ToInt64(),
                group.MemberIdentities.Order(StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();
        SettingsLayout[] layouts = Layout.EnumerateLayouts()
            .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
            .Select(layout => new SettingsLayout(
                layout.Identifier.ToInt64(), layout.LanguageName, layout.LayoutName))
            .ToArray();
        return new SettingsSnapshot(SettingsIpcProtocol.Version, deviceRows, groups, layouts);
    }

    private static void RequireIdentity(SettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Identity))
        {
            throw new InvalidOperationException("No se indicó un dispositivo.");
        }
    }

    private static string RequireFilePath(SettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new InvalidOperationException("No se indicó un archivo.");
        }

        return request.FilePath;
    }

    private static string RequireTargetIdentity(SettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetIdentity))
        {
            throw new InvalidOperationException("No se indicó la identidad que se agrupará.");
        }

        return request.TargetIdentity;
    }

    private static Layout? ResolveLayout(SettingsRequest request)
    {
        Layout? layout = request.LayoutIdentifier is long id
            ? Layout.EnumerateLayouts().FirstOrDefault(candidate => candidate.Identifier.ToInt64() == id)
            : null;
        if (request.LayoutIdentifier is not null && layout is null)
        {
            throw new InvalidOperationException("La distribución seleccionada ya no está disponible.");
        }

        return layout;
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
