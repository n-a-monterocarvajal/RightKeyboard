using Microsoft.Win32;

namespace RightKeyboard;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "RightKeyboard";

    public static bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            string? command = key?.GetValue(ValueName) as string;
            return string.Equals(command, BuildCommand(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (enabled)
        {
            key.SetValue(ValueName, BuildCommand(), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }

    private static string BuildCommand()
    {
        string executable = Environment.ProcessPath ?? Application.ExecutablePath;
        return $"\"{executable}\"";
    }
}
