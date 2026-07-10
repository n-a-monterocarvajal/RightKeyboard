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
            string[] commandLine = Environment.GetCommandLineArgs();
            int selectorArgument = Array.FindIndex(commandLine, argument => argument == "--select");
            window = selectorArgument >= 0 && selectorArgument + 1 < commandLine.Length
                ? new LayoutSelectionWindow(new SettingsIpcClient(), commandLine[selectorArgument + 1])
                : new SettingsWindow(new SettingsIpcClient());
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
