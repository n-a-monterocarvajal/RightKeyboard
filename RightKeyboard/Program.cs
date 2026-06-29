namespace RightKeyboard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        Application.Run(new TrayApplicationContext());
    }
}
