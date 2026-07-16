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
        DiagnosticLogger logger = new(directory, forceEnableForTests: true);
        logger.Write("entrada_recibida", CreateDevice());
        Assert.That(File.Exists(Path.Combine(directory, "rightkeyboard-diagnostico.log")), Is.False);
    }

    [Test]
    public void Enabled_AnonymizesDeviceIdentifiers()
    {
        DiagnosticLogger logger = new(directory, forceEnableForTests: true);
        KeyboardDevice device = CreateDevice();
        logger.SetDetailedEnabled(true);
        logger.Write("entrada_recibida", device, new { decision = "prueba" });
        logger.FlushAsync().GetAwaiter().GetResult();

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
    public void Enabled_DoesNotLeakDisplayNameOrRawFingerprint()
    {
        DiagnosticLogger logger = new(directory, forceEnableForTests: true);
        KeyboardDevice device = CreateDevice();
        logger.SetDetailedEnabled(true);
        logger.Write("entrada_recibida", device, new { keyCategory = "asignable", messageCategory = "normal" });
        logger.FlushAsync().GetAwaiter().GetResult();

        string[] lines = File.ReadAllLines(Path.Combine(directory, "rightkeyboard-diagnostico.log"));
        string log = string.Join('\n', lines);
        using JsonDocument document = JsonDocument.Parse(lines[1]);
        JsonElement loggedDevice = document.RootElement.GetProperty("device");
        Assert.Multiple(() =>
        {
            Assert.That(log, Does.Not.Contain(device.DisplayName));
            Assert.That(log, Does.Not.Contain(device.Fingerprint));
            Assert.That(loggedDevice.GetProperty("fingerprint").GetString(), Has.Length.EqualTo(16));
        });
    }

    [Test]
    public void Disable_StopsSubsequentEvents()
    {
        DiagnosticLogger logger = new(directory, forceEnableForTests: true);
        logger.SetDetailedEnabled(true);
        logger.SetDetailedEnabled(false);
        logger.FlushAsync().GetAwaiter().GetResult();
        string path = Path.Combine(directory, "rightkeyboard-diagnostico.log");
        int linesBefore = File.ReadAllLines(path).Length;
        logger.Write("no_debe_aparecer", CreateDevice());
        Assert.That(File.ReadAllLines(path), Has.Length.EqualTo(linesBefore));
    }

    [Test]
    public void Enabled_SignatureDetails_DoNotLeakRawPath()
    {
        DiagnosticLogger logger = new(directory, forceEnableForTests: true);
        KeyboardDevice device = CreateDevice();
        string signature = HidSignature.TryFromDevice(device.DevicePath, device.Capabilities)!.ToCanonicalString();
        logger.SetDetailedEnabled(true);
        logger.Write("firma_registrada", device, new { signature, origin = "selector" });
        logger.FlushAsync().GetAwaiter().GetResult();

        string log = string.Join('\n', File.ReadAllLines(Path.Combine(directory, "rightkeyboard-diagnostico.log")));
        Assert.Multiple(() =>
        {
            // El canónico solo contiene tokens públicos y puede registrarse en claro.
            Assert.That(log, Does.Contain(signature));
            // La parte privada del path y la huella cruda no deben aparecer.
            Assert.That(log, Does.Not.Contain("PRIVATE"));
            Assert.That(log, Does.Not.Contain(device.DisplayName));
        });
    }

    private static KeyboardDevice CreateDevice() => new(
        @"\\?\HID#VID_1234&PID_5678#PRIVATE", 42,
        "container:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        "FINGERPRINT-PRIVATE", "Teclado de prueba", "Dispositivo PRIVADO", false);
}
