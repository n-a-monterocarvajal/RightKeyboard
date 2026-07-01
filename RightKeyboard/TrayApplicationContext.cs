using System.Diagnostics;
using RightKeyboard.Win32;

namespace RightKeyboard;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly KeyboardDevicesCollection devices;
    private readonly Configuration configuration;
    private readonly RawInputWindow inputWindow;
    private readonly ContextMenuStrip menu;
    private readonly NotifyIcon notifyIcon;
    private readonly SynchronizationContext uiContext;
    private readonly PreferenceResetService preferenceReset;
    private readonly System.Windows.Forms.Timer selectionTimer;
    private readonly SettingsIpcServer settingsIpc;
    private KeyboardDevice? pendingDevice;
    private SettingsDialog? settingsDialog;
    private Process? settingsProcess;
    private bool selectingLayout;

    public TrayApplicationContext()
    {
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        devices = new KeyboardDevicesCollection();
        configuration = LoadConfiguration();
        preferenceReset = new PreferenceResetService(configuration);
        selectionTimer = new System.Windows.Forms.Timer { Interval = 100 };
        selectionTimer.Tick += OnSelectionTimerTick;
        settingsIpc = new SettingsIpcServer(configuration, devices, uiContext);

        menu = TrayMenuFactory.Create(ShowSettings, ClearPreferences, ExitThread);

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "RightKeyboard",
            Visible = true
        };
        notifyIcon.DoubleClick += (_, _) => ShowSettings();

        inputWindow = new RawInputWindow();
        inputWindow.KeyboardInput += OnKeyboardInput;
        inputWindow.DevicesChanged += OnDevicesChanged;
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
        catch
        {
            return;
        }

        if (settingsDialog is { IsDisposed: false } openSettings && keyboardEvent.CanStartMapping)
        {
            configuration.TouchDevice(device);
            openSettings.HighlightDevice(device.Identity);
            return;
        }

        int matchingDevices = devices.CountConnectedWithFingerprint(device.Fingerprint);
        if (configuration.IsIgnored(device, matchingDevices, out bool learnedIgnoredIdentity))
        {
            if (learnedIgnoredIdentity)
            {
                SaveConfiguration();
            }

            return;
        }

        if (device.IsClearlyNonKeyboard)
        {
            configuration.Ignore(device);
            SaveConfiguration();
            return;
        }

        if (configuration.TryGetLayout(device, matchingDevices, out Layout? layout, out bool learnedLayoutIdentity))
        {
            if (learnedLayoutIdentity)
            {
                SaveConfiguration();
            }

            ApplyLayout(layout!);
            return;
        }

        if (!keyboardEvent.CanStartMapping || pendingDevice is not null)
        {
            return;
        }

        pendingDevice = device;
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

    private void OnDevicesChanged() => devices.Refresh();

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
            settingsProcess = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
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

    private void ClearPreferences()
    {
        preferenceReset.TryClear();
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
        settingsDialog?.Close();
        settingsDialog?.Dispose();
        if (settingsProcess is { HasExited: false })
        {
            settingsProcess.CloseMainWindow();
        }
        settingsProcess?.Dispose();
        inputWindow.Dispose();
        notifyIcon.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
