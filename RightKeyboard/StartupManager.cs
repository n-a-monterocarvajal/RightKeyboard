using Microsoft.Win32;

namespace RightKeyboard;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "RightKeyboard";

    public static bool IsEnabled =>
        IsEnabledCore(RunKeyPath, StartupApprovedKeyPath, ValueName, BuildCommand());

    public static void SetEnabled(bool enabled) =>
        SetEnabledCore(RunKeyPath, StartupApprovedKeyPath, ValueName, BuildCommand(), enabled);

    // Núcleo verificable: opera sobre HKCU con rutas/valor/comando explícitos para
    // poder aislarlo en pruebas sin tocar la clave Run real del usuario. Los miembros
    // públicos delegan aquí con las constantes reales.
    internal static bool IsEnabledCore(
        string runKeyPath,
        string approvedKeyPath,
        string valueName,
        string command)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(runKeyPath);
        if (key?.GetValue(valueName) is not string stored ||
            !string.Equals(stored, command, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using RegistryKey? approvalKey = Registry.CurrentUser.OpenSubKey(approvedKeyPath);
        return approvalKey?.GetValue(valueName) is not byte[] approval || IsApproved(approval);
    }

    internal static void SetEnabledCore(
        string runKeyPath,
        string approvedKeyPath,
        string valueName,
        string command,
        bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(runKeyPath, true);
        if (enabled)
        {
            using RegistryKey? approvalKey = Registry.CurrentUser.OpenSubKey(approvedKeyPath, true);
            approvalKey?.DeleteValue(valueName, false);
            key.SetValue(valueName, command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(valueName, false);
        }
    }

    private static bool IsApproved(byte[] approval) => approval.Length == 0 || approval[0] != 3;

    private static string BuildCommand()
    {
        string executable = Environment.ProcessPath ?? Application.ExecutablePath;
        return $"\"{executable}\"";
    }
}
