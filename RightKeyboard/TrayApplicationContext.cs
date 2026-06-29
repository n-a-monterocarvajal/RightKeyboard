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
    private string? pendingIdentity;
    private bool selectingLayout;

    public TrayApplicationContext()
    {
        uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        devices = new KeyboardDevicesCollection();
        configuration = LoadConfiguration();

        menu = new ContextMenuStrip();
        menu.Items.Add("Limpiar preferencias", null, (_, _) => ClearPreferences());
        menu.Items.Add("Salir", null, (_, _) => ExitThread());

        notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "RightKeyboard",
            Visible = true
        };

        inputWindow = new RawInputWindow();
        inputWindow.KeyboardInput += OnKeyboardInput;
    }

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

        string identity;
        try
        {
            identity = devices.GetIdentity(keyboardEvent.DeviceHandle);
        }
        catch
        {
            return;
        }

        if (configuration.LayoutMappings.TryGetValue(identity, out Layout? layout))
        {
            ApplyLayout(layout);
            return;
        }

        if (!keyboardEvent.CanStartMapping || pendingIdentity is not null)
        {
            return;
        }

        pendingIdentity = identity;
        uiContext.Post(_ => SelectLayoutForPendingDevice(), null);
    }

    private void SelectLayoutForPendingDevice()
    {
        string? identity = pendingIdentity;
        pendingIdentity = null;
        if (identity is null || selectingLayout)
        {
            return;
        }

        selectingLayout = true;
        try
        {
            using LayoutSelectionDialog dialog = new();
            if (dialog.ShowDialog() != DialogResult.OK || dialog.SelectedLayout is null)
            {
                return;
            }

            configuration.LayoutMappings[identity] = dialog.SelectedLayout;
            SaveConfiguration();
            ApplyLayout(dialog.SelectedLayout);
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
        try
        {
            configuration.Clear();
        }
        catch (Exception error)
        {
            ShowSaveError(error);
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
        notifyIcon.Visible = false;
        inputWindow.KeyboardInput -= OnKeyboardInput;
        inputWindow.Dispose();
        notifyIcon.Dispose();
        menu.Dispose();
        base.ExitThreadCore();
    }
}
