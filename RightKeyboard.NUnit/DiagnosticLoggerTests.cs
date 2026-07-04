using System.Text.Json;
using NUnit.Framework;

namespace RightKeyboard.Tests;

public sealed class DiagnosticLoggerTests
{
    private string directory = null!;

    [SetUp]
    public void SetUp() =>
        directory = Path.Combine(Path.GetTempPath(), "RightKeyboard.Tests", Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void Disabled_DoesNotCreateLog()
    {
        DiagnosticLogger logger = new(directory);
        logger.Write("entrada_recibida", CreateDevice());
        Assert.That(File.Exists(Path.Combine(directory, "rightkeyboard-diagnostico.log")), Is.False);
    }

    [Test]
    public void Enabled_AnonymizesDeviceIdentifiers()
    {
        DiagnosticLogger logger = new(directory);
        KeyboardDevice device = CreateDevice();
        logger.SetDetailedEnabled(true);
        logger.Write("entrada_recibida", device, new { decision = "prueba" });

        string[] lines = File.ReadAllLines(Path.Combine(directory, "rightkeyboard-diagnostico.log"));
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(string.Join('\n', lines), Does.Not.Contain(device.Identity));
        Assert.That(string.Join('\n', lines), Does.Not.Contain(device.DevicePath));
        using JsonDocument document = JsonDocument.Parse(lines[1]);
        JsonElement loggedDevice = document.RootElement.GetProperty("device");
        Assert.That(loggedDevice.GetProperty("identity").GetString(), Has.Length.EqualTo(16));
        Assert.That(loggedDevice.GetProperty("path").GetString(), Has.Length.EqualTo(16));
        Assert.That(loggedDevice.GetProperty("identityKind").GetString(), Is.EqualTo("container"));
        Assert.That(loggedDevice.GetProperty("vendorId").GetString(), Is.EqualTo("1234"));
        Assert.That(loggedDevice.GetProperty("productId").GetString(), Is.EqualTo("5678"));
    }

    [Test]
    public void Disable_StopsSubsequentEvents()
    {
        DiagnosticLogger logger = new(directory);
        logger.SetDetailedEnabled(true);
        logger.SetDetailedEnabled(false);
        string path = Path.Combine(directory, "rightkeyboard-diagnostico.log");
        int linesBefore = File.ReadAllLines(path).Length;
        logger.Write("no_debe_aparecer", CreateDevice());
        Assert.That(File.ReadAllLines(path), Has.Length.EqualTo(linesBefore));
    }

    private static KeyboardDevice CreateDevice() => new(
        @"\\?\HID#VID_1234&PID_5678#PRIVATE", 42,
        "container:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        "FINGERPRINT-PRIVATE", "Teclado de prueba", "Dispositivo PRIVADO", false);
}
