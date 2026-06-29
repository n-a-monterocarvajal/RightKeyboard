using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class ConfigurationTests
{
    private string temporaryDirectory = null!;
    private Layout spanish = null!;

    [SetUp]
    public void SetUp()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "RightKeyboard.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        spanish = new Layout(new nint(0x0000040A), "español (Chile)", "Latinoamericano");
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(temporaryDirectory, true);
    }

    [Test]
    public void SaveAndLoad_PreservesAliasMappingAndIgnoredDevice()
    {
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        Configuration configuration = new();
        KeyboardDevice keyboard = Device("container:keyboard", "MODEL-A", "Teclado sin nombre");
        KeyboardDevice mouse = Device("container:mouse", "MODEL-M", "MX Master 3S", nonKeyboard: true);
        configuration.SetLayout(keyboard, spanish, "Teclado oficina");
        configuration.Ignore(mouse, "Mouse principal");

        configuration.Save(path);
        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.LayoutMappings[keyboard.Identity].Identifier, Is.EqualTo(spanish.Identifier));
            Assert.That(loaded.Devices[keyboard.Identity].CustomName, Is.EqualTo("Teclado oficina"));
            Assert.That(loaded.Devices[keyboard.Identity].DetectedName, Is.EqualTo("Teclado sin nombre"));
            Assert.That(loaded.IgnoredDevices.Contains(mouse.Identity), Is.True);
            Assert.That(loaded.Devices[mouse.Identity].CustomName, Is.EqualTo("Mouse principal"));
            Assert.That(File.ReadAllText(path), Does.Contain("\"version\": 3"));
        });
    }

    [Test]
    public void Version2File_IsMigratedInMemory()
    {
        string path = Path.Combine(temporaryDirectory, "preferences-v2.json");
        File.WriteAllText(path, """
        {
          "version": 2,
          "mappings": [
            {
              "identity": "container:old",
              "fingerprint": "MODEL-A",
              "displayName": "Teclado anterior",
              "layout": "000000000000040A"
            }
          ],
          "ignoredDevices": []
        }
        """);

        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Devices["container:old"].DetectedName, Is.EqualTo("Teclado anterior"));
            Assert.That(loaded.LayoutMappings["container:old"], Is.SameAs(spanish));
        });
    }

    [Test]
    public void ReconnectedUniqueDevice_RecoversLayoutAndAliasFromFingerprint()
    {
        Configuration configuration = new();
        configuration.SetLayout(Device("container:old", "MODEL-A", "Teclado"), spanish, "Mi teclado");

        bool found = configuration.TryGetLayout(
            Device("container:new", "MODEL-A", "Teclado"),
            connectedDevicesWithSameFingerprint: 1,
            out Layout? layout,
            out bool learnedIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(layout, Is.SameAs(spanish));
            Assert.That(learnedIdentity, Is.True);
            Assert.That(configuration.Devices["container:new"].CustomName, Is.EqualTo("Mi teclado"));
        });
    }

    [Test]
    public void TwoIdenticalConnectedDevices_DoNotGuessLayout()
    {
        Configuration configuration = new();
        configuration.SetLayout(Device("container:old", "MODEL-A", "Teclado"), spanish);

        bool found = configuration.TryGetLayout(
            Device("container:new", "MODEL-A", "Teclado"),
            connectedDevicesWithSameFingerprint: 2,
            out _,
            out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void UpdatePreference_CanRenameChangeLayoutAndIgnore()
    {
        Configuration configuration = new();
        KeyboardDevice keyboard = Device("device:keyboard", "MODEL-A", "Teclado");
        configuration.SetLayout(keyboard, spanish);

        configuration.UpdatePreference(keyboard.Identity, "Teclado viaje", null, ignored: true);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Devices[keyboard.Identity].CustomName, Is.EqualTo("Teclado viaje"));
            Assert.That(configuration.IgnoredDevices.Contains(keyboard.Identity), Is.True);
            Assert.That(configuration.LayoutMappings.ContainsKey(keyboard.Identity), Is.False);
        });
    }

    [Test]
    public void MergeFrom_CanCombineOrReplace()
    {
        Configuration current = new();
        current.SetLayout(Device("device:a", "MODEL-A", "A"), spanish, "A local");
        Configuration imported = new();
        imported.Ignore(Device("device:b", "MODEL-B", "B"), "B importado");

        current.MergeFrom(imported, replace: false);
        Assert.That(current.Devices.Keys, Is.EquivalentTo(new[] { "device:a", "device:b" }));

        current.MergeFrom(imported, replace: true);
        Assert.Multiple(() =>
        {
            Assert.That(current.Devices.Keys, Is.EquivalentTo(new[] { "device:b" }));
            Assert.That(current.IgnoredDevices.Contains("device:b"), Is.True);
        });
    }

    [Test]
    public void Clear_RemovesAllDeviceData()
    {
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        Configuration configuration = new();
        configuration.SetLayout(Device("device:keyboard", "MODEL-A", "Teclado"), spanish);
        configuration.Ignore(Device("device:mouse", "MODEL-M", "Mouse", nonKeyboard: true));

        configuration.Clear(path);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Devices, Is.Empty);
            Assert.That(configuration.LayoutMappings, Is.Empty);
            Assert.That(configuration.IgnoredDevices, Is.Empty);
            Assert.That(File.ReadAllText(path), Does.Contain("\"devices\": []"));
        });
    }

    private static KeyboardDevice Device(
        string identity,
        string fingerprint,
        string displayName,
        bool nonKeyboard = false) =>
        new("test-path", 1, identity, fingerprint, displayName, "ID DE PRUEBA", nonKeyboard);
}
