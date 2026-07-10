using System.Diagnostics;
using RightKeyboard.Win32;

namespace RightKeyboard;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly KeyboardDevicesCollection devices;
    private readonly Configuration configuration;
    private readonly RawInputWindow inputWindow;
    private readonly NativeTrayMenu menu;
    private readonly NotifyIcon notifyIcon;
    private readonly SynchronizationContext uiContext;
    private readonly System.Windows.Forms.Timer selectionTimer;
    private readonly SettingsIpcServer settingsIpc;
    private readonly DiagnosticLogger diagnostics;
    private KeyboardDevice? pendingDevice;
    private SettingsDialog? settingsDialog;
    private Process? settingsProcess;
    private Process? selectionProcess;
    private KeyboardDevice? activeSelectionDevice;
    private bool selectingLayout;

    public TrayApplicationContext()
    {
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        devices = new KeyboardDevicesCollection();
        configuration = LoadConfiguration();
        selectionTimer = new System.Windows.Forms.Timer { Interval = 100 };
        selectionTimer.Tick += OnSelectionTimerTick;
        diagnostics = new DiagnosticLogger();
        settingsIpc = new SettingsIpcServer(configuration, devices, uiContext, diagnostics);
        diagnostics.Write("aplicacion_iniciada", details: new
        {
            os = Environment.OSVersion.VersionString,
            architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            connectedDevices = devices.Count()
        });

        inputWindow = new RawInputWindow();
        inputWindow.KeyboardInput += OnKeyboardInput;
        inputWindow.DevicesChanged += OnDevicesChanged;
        menu = new NativeTrayMenu(inputWindow.Handle, ShowSettings, ExitThread);

        notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "RightKeyboard",
            Visible = true
        };
        notifyIcon.DoubleClick += (_, _) => ShowSettings();
        notifyIcon.MouseUp += (_, args) =>
        {
            if (args.Button == MouseButtons.Right)
            {
                menu.Show();
            }
        };
    }

    public void RequestExit() => uiContext.Post(_ => ExitThread(), null);

    private Configuration LoadConfiguration()
    {
        try
        {
            return Configuration.LoadConfiguration(devices);
        }
        catch (Exception error)
        {
            MessageBox.Show(
                $"No se pudo cargar la configuración.\n\n{error.Message}",
                "RightKeyboard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return new Configuration();
        }
    }

    private void OnKeyboardInput(RawKeyboardEvent keyboardEvent)
    {
        if (!keyboardEvent.IsKeyDown || selectingLayout)
        {
            return;
        }

        KeyboardDevice device;
        try
        {
            device = devices.GetDevice(keyboardEvent.DeviceHandle);
        }
        catch (Exception error)
        {
            diagnostics.Write("entrada_sin_dispositivo", details: new { error = error.GetType().Name });
            return;
        }

        int matchingDevices = devices.CountConnectedWithFingerprint(device.Fingerprint);
        bool knownIdentity = configuration.Devices.ContainsKey(device.Identity);
        bool mappedIdentity = configuration.LayoutMappings.ContainsKey(device.Identity);
        bool ignoredIdentity = configuration.IgnoredDevices.Contains(device.Identity);
        diagnostics.Write("entrada_recibida", device, new
        {
            keyCategory = keyboardEvent.CanStartMapping ? "asignable" : "auxiliar_o_modificadora",
            messageCategory = keyboardEvent.IsSystemKeyDown ? "sistema" : "normal",
            keyboardEvent.Flags,
            keyboardEvent.HasScanCode,
            keyboardEvent.HasExtraInformation,
            keyboardEvent.IsExtendedKey,
            knownIdentity,
            mappedIdentity,
            ignoredIdentity,
            matchingFingerprintDevices = matchingDevices,
            settingsOpen = settingsProcess is { HasExited: false }
        });

        if (!keyboardEvent.HasScanCode && DeviceClassifier.IsLikelySyntheticInputSource(device))
        {
            diagnostics.Write("entrada_sintetica_excluida", device, new
            {
                reason = "sin_scan_code_y_capacidades",
                keyboardEvent.VirtualKey
            });
            return;
        }

        if (settingsProcess is { HasExited: false })
        {
            settingsIpc.NotifyDeviceInput(device.Identity);
        }

        if (settingsDialog is { IsDisposed: false } openSettings && keyboardEvent.CanStartMapping)
        {
            configuration.TouchDevice(device);
            openSettings.HighlightDevice(device.Identity);
            return;
        }

        if (configuration.IsIgnored(device, matchingDevices, out bool learnedIgnoredIdentity))
        {
            diagnostics.Write(learnedIgnoredIdentity ? "ignorado_recuperado_por_huella" : "entrada_ignorada", device);
            if (learnedIgnoredIdentity)
            {
                SaveConfiguration();
            }

            return;
        }

        if (device.IsClearlyNonKeyboard)
        {
            diagnostics.Write("dispositivo_excluido_por_clasificacion", device);
            configuration.Ignore(device);
            SaveConfiguration();
            return;
        }

        if (configuration.TryGetLayout(device, matchingDevices, out Layout? layout, out bool learnedLayoutIdentity))
        {
            diagnostics.Write(learnedLayoutIdentity ? "distribucion_recuperada_por_huella" : "distribucion_aplicada", device, new
            {
                layout = layout!.Identifier.ToInt64().ToString("X")
            });
            if (learnedLayoutIdentity)
            {
                SaveConfiguration();
            }

            ApplyLayout(layout!);
            return;
        }

        // Configuración debe poder recibir texto normalmente. Mientras el frontend
        // WinUI está abierto observamos el dispositivo, pero no lanzamos el selector.
        if (settingsProcess is { HasExited: false })
        {
            if (keyboardEvent.CanStartMapping)
            {
                configuration.TouchDevice(device);
            }

            diagnostics.Write("selector_omitido_configuracion_abierta", device, new
            {
                mappingCandidate = keyboardEvent.CanStartMapping
            });

            return;
        }

        if (!keyboardEvent.CanStartMapping || pendingDevice is not null)
        {
            return;
        }

        pendingDevice = device;
        diagnostics.Write("selector_programado", device, new
        {
            knownIdentity,
            matchingFingerprintDevices = matchingDevices
        });
        selectionTimer.Stop();
        selectionTimer.Start();
    }

    private void OnSelectionTimerTick(object? sender, EventArgs e)
    {
        selectionTimer.Stop();
        SelectLayoutForPendingDevice();
    }

    private void SelectLayoutForPendingDevice()
    {
        KeyboardDevice? queuedDevice = pendingDevice;
        pendingDevice = null;
        if (queuedDevice is not KeyboardDevice device || selectingLayout)
        {
            return;
        }

        selectingLayout = true;
        if (TryLaunchWinUiSelector(device))
        {
            return;
        }

        try
        {
            using LayoutSelectionDialog dialog = new(device, configuration.GetDisplayName(device));
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.Ignore)
            {
                configuration.Ignore(device, dialog.CustomName);
                SaveConfiguration();
                return;
            }

            if (result != DialogResult.OK || dialog.SelectedLayout is null)
            {
                return;
            }

            configuration.SetLayout(device, dialog.SelectedLayout, dialog.CustomName);
            SaveConfiguration();
            ApplyLayout(dialog.SelectedLayout);
        }
        finally
        {
            selectingLayout = false;
        }
    }

    private bool TryLaunchWinUiSelector(KeyboardDevice device)
    {
        string? executable = FindWinUiExecutable();
        if (executable is null)
        {
            return false;
        }

        try
        {
            ProcessStartInfo startInfo = new(executable) { UseShellExecute = false };
            startInfo.ArgumentList.Add("--select");
            startInfo.ArgumentList.Add(device.Identity);
            selectionProcess = Process.Start(startInfo);
            if (selectionProcess is null)
            {
                return false;
            }

            activeSelectionDevice = device;
            API.AllowSetForegroundWindow((uint)selectionProcess.Id);
            selectionProcess.Exited += (_, _) => uiContext.Post(_ => CompleteWinUiSelection(), null);
            selectionProcess.EnableRaisingEvents = true;
            return true;
        }
        catch
        {
            selectionProcess?.Dispose();
            selectionProcess = null;
            activeSelectionDevice = null;
            return false;
        }
    }

    private void CompleteWinUiSelection()
    {
        KeyboardDevice? device = activeSelectionDevice;
        activeSelectionDevice = null;
        selectionProcess?.Dispose();
        selectionProcess = null;
        try
        {
            if (device is KeyboardDevice selectedDevice)
            {
                int matchingDevices = devices.CountConnectedWithFingerprint(selectedDevice.Fingerprint);
                if (configuration.TryGetLayout(selectedDevice, matchingDevices, out Layout? layout, out _))
                {
                    ApplyLayout(layout!);
                }
            }
        }
        finally
        {
            selectingLayout = false;
        }
    }

    private void OnDevicesChanged()
    {
        devices.Refresh();
        diagnostics.Write("inventario_dispositivos_actualizado", details: new
        {
            connectedDevices = devices.Count(),
            identities = devices.Select(device => DiagnosticLogger.Anonymize(device.Identity)).Order().ToArray(),
            fingerprints = devices.Select(device => DiagnosticLogger.Anonymize(device.Fingerprint)).Order().ToArray()
        });
    }

    private void ShowSettings()
    {
        if (selectingLayout || settingsDialog is not null || settingsProcess is { HasExited: false })
        {
            return;
        }

        if (TryLaunchWinUiSettings())
        {
            return;
        }

        try
        {
            settingsDialog = new SettingsDialog(configuration, devices, SaveConfiguration);
            settingsDialog.ShowDialog();
        }
        finally
        {
            settingsDialog?.Dispose();
            settingsDialog = null;
        }
    }

    private bool TryLaunchWinUiSettings()
    {
        string? executable = FindWinUiExecutable();
        if (executable is null)
        {
            return false;
        }

        try
        {
            settingsProcess = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = false });
            return settingsProcess is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindWinUiExecutable()
    {
        string installed = Path.Combine(AppContext.BaseDirectory, "ui", "RightKeyboard.WinUI.exe");
        if (File.Exists(installed))
        {
            return installed;
        }

#if DEBUG
        string development = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "RightKeyboard.WinUI", "bin", "x64", "Debug",
            "net10.0-windows10.0.19041.0", "win-x64", "RightKeyboard.WinUI.exe"));
        if (File.Exists(development))
        {
            return development;
        }
#endif
        return null;
    }

    private static void ApplyLayout(Layout layout)
    {
        if (!API.IsForegroundLayout(layout.Identifier))
        {
            API.RequestForegroundLayout(layout.Identifier);
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            configuration.Save();
        }
        catch (Exception error)
        {
            ShowSaveError(error);
        }
    }

    private static void ShowSaveError(Exception error) => MessageBox.Show(
        $"No se pudieron guardar las preferencias.\n\n{error.Message}",
        "RightKeyboard",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);

    protected override void ExitThreadCore()
    {
        SaveConfiguration();
        notifyIcon.Visible = false;
        inputWindow.KeyboardInput -= OnKeyboardInput;
        inputWindow.DevicesChanged -= OnDevicesChanged;
        selectionTimer.Stop();
        selectionTimer.Tick -= OnSelectionTimerTick;
        selectionTimer.Dispose();
        settingsIpc.Dispose();
        diagnostics.Dispose();
        settingsDialog?.Close();
        settingsDialog?.Dispose();
        if (settingsProcess is { HasExited: false })
        {
            settingsProcess.CloseMainWindow();
        }
        settingsProcess?.Dispose();
        if (selectionProcess is { HasExited: false })
        {
            selectionProcess.CloseMainWindow();
        }
        selectionProcess?.Dispose();
        inputWindow.Dispose();
        notifyIcon.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
