using Microsoft.UI.Xaml;

namespace RightKeyboard.WinUI;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            KeyboardDevicesCollection devices = new();
            Configuration configuration = Configuration.LoadConfiguration(devices);
            window = new SettingsWindow(configuration, devices);
        }
        catch (Exception error)
        {
            window = new Window
            {
                Title = "RightKeyboard WinUI — error de inicio",
                Content = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Margin = new Thickness(24),
                    Text = error.ToString(),
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        window.Activate();
    }
}
