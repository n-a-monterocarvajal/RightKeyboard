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
    private KeyboardDevice? pendingDevice;
    private bool selectingLayout;

    public TrayApplicationContext()
    {
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        devices = new KeyboardDevicesCollection();
        configuration = LoadConfiguration();
        preferenceReset = new PreferenceResetService(configuration);

        menu = new FluentContextMenuStrip
        {
            Renderer = new ModernMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(4),
            Font = SystemFonts.MessageBoxFont,
            AccessibleName = "Menú de RightKeyboard"
        };
        ToolStripMenuItem settingsItem = new("&Configuración", null, (_, _) => ShowSettings())
        {
            AccessibleName = "Configuración",
            AccessibleDescription = "Abre la edición de dispositivos y preferencias."
        };
        ToolStripMenuItem clearItem = new("&Limpiar preferencias", null, (_, _) => ClearPreferences())
        {
            AccessibleName = "Limpiar preferencias",
            AccessibleDescription = "Elimina las preferencias guardadas después de pedir confirmación."
        };
        ToolStripMenuItem exitItem = new("&Salir", null, (_, _) => ExitThread())
        {
            AccessibleName = "Salir",
            AccessibleDescription = "Cierra RightKeyboard."
        };
        menu.Items.Add(settingsItem);
        menu.Items.Add(clearItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);
        foreach (ToolStripMenuItem item in menu.Items.OfType<ToolStripMenuItem>())
        {
            item.Padding = new Padding(8, 5, 18, 5);
            item.AutoToolTip = false;
        }

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
        uiContext.Post(_ => SelectLayoutForPendingDevice(), null);
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
        if (selectingLayout)
        {
            return;
        }

        selectingLayout = true;
        try
        {
            using SettingsDialog dialog = new(configuration, devices, SaveConfiguration);
            dialog.ShowDialog();
        }
        finally
        {
            selectingLayout = false;
        }
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
        inputWindow.Dispose();
        notifyIcon.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
