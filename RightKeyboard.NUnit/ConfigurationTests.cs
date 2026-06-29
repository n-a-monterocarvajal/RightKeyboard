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
    public void SaveAndLoad_PreservesMappingsAndIgnoredDevices()
    {
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        Configuration configuration = new();
        KeyboardDevice keyboard = Device("container:keyboard", "MODEL-A", "Teclado principal");
        KeyboardDevice mouse = Device("container:mouse", "MODEL-M", "MX Master 3S", nonKeyboard: true);
        configuration.SetLayout(keyboard, spanish);
        configuration.Ignore(mouse);

        configuration.Save(path);
        Configuration loaded = Configuration.LoadConfiguration(
            new KeyboardDevicesCollection(),
            path,
            [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.LayoutMappings[keyboard.Identity].Layout.Identifier, Is.EqualTo(spanish.Identifier));
            Assert.That(loaded.LayoutMappings[keyboard.Identity].DisplayName, Is.EqualTo("Teclado principal"));
            Assert.That(loaded.IgnoredDevices.ContainsKey(mouse.Identity), Is.True);
            Assert.That(File.ReadAllText(path), Does.Contain("\"version\": 2"));
        });
    }

    [Test]
    public void ReconnectedUniqueDevice_RecoversLayoutFromFingerprint()
    {
        Configuration configuration = new();
        configuration.SetLayout(Device("container:old", "MODEL-A", "Teclado"), spanish);

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
            Assert.That(configuration.LayoutMappings.ContainsKey("container:new"), Is.True);
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
    public void IgnoredDevice_RecoversDecisionAfterIdentityChanges()
    {
        Configuration configuration = new();
        configuration.Ignore(Device("container:old", "MODEL-M", "MX Master 3S", nonKeyboard: true));

        bool ignored = configuration.IsIgnored(
            Device("container:new", "MODEL-M", "MX Master 3S", nonKeyboard: true),
            connectedDevicesWithSameFingerprint: 1,
            out bool learnedIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(ignored, Is.True);
            Assert.That(learnedIdentity, Is.True);
            Assert.That(configuration.IgnoredDevices.ContainsKey("container:new"), Is.True);
        });
    }

    [Test]
    public void Clear_RemovesMappingsAndIgnoredDevices()
    {
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        Configuration configuration = new();
        configuration.SetLayout(Device("device:keyboard", "MODEL-A", "Teclado"), spanish);
        configuration.Ignore(Device("device:mouse", "MODEL-M", "Mouse", nonKeyboard: true));

        configuration.Clear(path);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.LayoutMappings, Is.Empty);
            Assert.That(configuration.IgnoredDevices, Is.Empty);
            Assert.That(File.ReadAllText(path), Does.Contain("\"mappings\": []"));
            Assert.That(File.ReadAllText(path), Does.Contain("\"ignoredDevices\": []"));
        });
    }

    private static KeyboardDevice Device(
        string identity,
        string fingerprint,
        string displayName,
        bool nonKeyboard = false) =>
        new("test-path", 1, identity, fingerprint, displayName, "ID DE PRUEBA", nonKeyboard);
}
