using Microsoft.Win32;
using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class StartupManagerTests
{
    private const string ValueName = "RightKeyboard";
    private const string Command = "\"C:\\Program Files\\Right Keyboard\\RightKeyboard.exe\"";
    private string root = null!;
    private string runKeyPath = null!;
    private string approvedKeyPath = null!;

    [SetUp]
    public void SetUp()
    {
        // Subclave desechable bajo HKCU: nunca toca la clave Run real del usuario.
        root = $@"Software\RightKeyboard.Tests\{Guid.NewGuid():N}";
        runKeyPath = root + @"\Run";
        approvedKeyPath = root + @"\StartupApproved\Run";
    }

    [TearDown]
    public void TearDown()
    {
        Registry.CurrentUser.DeleteSubKeyTree(root, throwOnMissingSubKey: false);
    }

    [Test]
    public void Enable_IsIdempotentAndPreservesCommandWithSpaces()
    {
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: true);
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: true);

        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(runKeyPath);
        Assert.Multiple(() =>
        {
            Assert.That(StartupManager.IsEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command), Is.True);
            Assert.That(run?.GetValue(ValueName), Is.EqualTo(Command));
        });
    }

    [Test]
    public void Disable_IsIdempotentAndClearsValue()
    {
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: true);
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: false);
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: false);

        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(runKeyPath);
        Assert.Multiple(() =>
        {
            Assert.That(StartupManager.IsEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command), Is.False);
            Assert.That(run?.GetValue(ValueName), Is.Null);
        });
    }

    [Test]
    public void IsEnabled_IsFalse_WhenStoredCommandDiffers()
    {
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: true);

        Assert.That(
            StartupManager.IsEnabledCore(runKeyPath, approvedKeyPath, ValueName, "\"C:\\otra ruta\\RightKeyboard.exe\""),
            Is.False);
    }

    [Test]
    public void IsEnabled_IsFalse_WhenStartupApprovedDisablesIt()
    {
        StartupManager.SetEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command, enabled: true);
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(approvedKeyPath, true))
        {
            // El primer byte 3 marca "deshabilitado por el usuario" en StartupApproved.
            approved.SetValue(
                ValueName,
                new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                RegistryValueKind.Binary);
        }

        Assert.That(StartupManager.IsEnabledCore(runKeyPath, approvedKeyPath, ValueName, Command), Is.False);
    }
}
