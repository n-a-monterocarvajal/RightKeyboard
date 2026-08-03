// Basado en la plantilla WinUI Unit Test App y el ejemplo unpackaged de Microsoft (licencia MIT).
using Microsoft.Testing.Platform.Builder;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace RightKeyboard.WinUI.Tests;

public partial class UnitTestApp : Application
{
    private Window? window;

    public UnitTestApp() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new UnitTestAppWindow();
        window.Activate();
        UITestMethodAttribute.DispatcherQueue = window.DispatcherQueue;

        try
        {
            string[] commandLine = Environment.GetCommandLineArgs()
                .Skip(1)
                .Where(argument => !argument.Contains("EnableMSTestRunner", StringComparison.Ordinal))
                .ToArray();
            ITestApplicationBuilder builder = await TestApplication.CreateBuilderAsync(commandLine);
            builder.AddSelfRegisteredExtensions(commandLine);
            using ITestApplication application = await builder.BuildAsync();
            Environment.ExitCode = await application.RunAsync();
        }
        finally
        {
            window.Close();
            Exit();
        }
    }
}
