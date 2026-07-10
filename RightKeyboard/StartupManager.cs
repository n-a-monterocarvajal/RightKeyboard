using Microsoft.Win32;

namespace RightKeyboard;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "RightKeyboard";

    public static bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            string? command = key?.GetValue(ValueName) as string;
            if (!string.Equals(command, BuildCommand(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using RegistryKey? approvalKey = Registry.CurrentUser.OpenSubKey(StartupApprovedKeyPath);
            return approvalKey?.GetValue(ValueName) is not byte[] approval || IsApproved(approval);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (enabled)
        {
            using RegistryKey? approvalKey = Registry.CurrentUser.OpenSubKey(StartupApprovedKeyPath, true);
            approvalKey?.DeleteValue(ValueName, false);
            key.SetValue(ValueName, BuildCommand(), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }

    private static bool IsApproved(byte[] approval) => approval.Length == 0 || approval[0] != 3;

    private static string BuildCommand()
    {
        string executable = Environment.ProcessPath ?? Application.ExecutablePath;
        return $"\"{executable}\"";
    }
}
