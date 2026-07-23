using Microsoft.UI.Xaml;
using RightKeyboard;

namespace RightKeyboard.WinUI;

public partial class App : Application
{
    private Window? window;
    private bool interactiveReached;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
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
            window.Activate();
            interactiveReached = true;
        }
        catch
        {
            // El frontend arrancó pero no llegó a ser usable. En lugar de mostrar
            // una ventana de error indistinguible para el residente, se termina con
            // el código acordado para que el núcleo abra el fallback WinForms.
            Environment.Exit(FrontendExitCodes.StartupFailure);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Una excepción no controlada antes de que la ventana sea interactiva
        // significa que el frontend nunca fue usable: se emite la misma señal de
        // fallo de arranque. Después de ser interactivo el usuario ya dispuso de
        // una interfaz, así que no se fuerza la salida ni el fallback.
        if (!interactiveReached)
        {
            e.Handled = true;
            Environment.Exit(FrontendExitCodes.StartupFailure);
        }
    }
}
