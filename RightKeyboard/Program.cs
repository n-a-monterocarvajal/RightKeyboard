namespace RightKeyboard;

internal static class Program
{
    internal const string InstanceMutexName = @"Local\RightKeyboard.SingleInstance";
    internal const string CloseEventName = @"Local\RightKeyboard.Close";

    [STAThread]
    private static void Main()
    {
        using Mutex instanceMutex = new(true, InstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using EventWaitHandle closeEvent = new(false, EventResetMode.AutoReset, CloseEventName);
        using TrayApplicationContext context = new();
        RegisteredWaitHandle closeRegistration = ThreadPool.RegisterWaitForSingleObject(
            closeEvent,
            (_, _) => context.RequestExit(),
            null,
            Timeout.Infinite,
            executeOnlyOnce: true);

        try
        {
            Application.Run(context);
        }
        finally
        {
            closeRegistration.Unregister(null);
        }
    }
}
