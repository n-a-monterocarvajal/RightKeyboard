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
        DateTimeOffset keyboardLastSeen = configuration.Devices[keyboard.Identity].LastSeenUtc;

        configuration.Save(path);
        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.LayoutMappings[keyboard.Identity].Identifier, Is.EqualTo(spanish.Identifier));
            Assert.That(loaded.Devices[keyboard.Identity].CustomName, Is.EqualTo("Teclado oficina"));
            Assert.That(loaded.Devices[keyboard.Identity].DetectedName, Is.EqualTo("Teclado sin nombre"));
            Assert.That(loaded.Devices[keyboard.Identity].Fingerprint, Is.EqualTo("MODEL-A"));
            Assert.That(loaded.Devices[keyboard.Identity].TechnicalId, Is.EqualTo("ID DE PRUEBA"));
            Assert.That(loaded.Devices[keyboard.Identity].LastSeenUtc, Is.EqualTo(keyboardLastSeen));
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
        DevicePreference original = configuration.Devices[keyboard.Identity];

        configuration.UpdatePreference(keyboard.Identity, "Teclado viaje", null, ignored: true);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Devices[keyboard.Identity], Is.SameAs(original));
            Assert.That(configuration.Devices[keyboard.Identity].CustomName, Is.EqualTo("Teclado viaje"));
            Assert.That(configuration.Devices[keyboard.Identity].Fingerprint, Is.EqualTo("MODEL-A"));
            Assert.That(configuration.Devices[keyboard.Identity].TechnicalId, Is.EqualTo("ID DE PRUEBA"));
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
    public void MergeFrom_ImportedDeviceWithoutModeClearsPreviousMode()
    {
        Configuration current = new();
        KeyboardDevice keyboard = Device("device:a", "MODEL-A", "A");
        current.SetLayout(keyboard, spanish, "Alias local");
        Configuration imported = new();
        imported.TouchDevice(keyboard, "Alias importado");

        current.MergeFrom(imported, replace: false);

        Assert.Multiple(() =>
        {
            Assert.That(current.Devices[keyboard.Identity].CustomName, Is.EqualTo("Alias importado"));
            Assert.That(current.LayoutMappings.ContainsKey(keyboard.Identity), Is.False);
            Assert.That(current.IgnoredDevices.Contains(keyboard.Identity), Is.False);
        });
    }

    [Test]
    public void LoadImport_RejectsFutureSchemaWithoutReadingItAsVersion3()
    {
        string path = WriteJson("future.json", """
        { "version": 4, "devices": [], "mappings": [], "ignoredDeviceIds": [] }
        """);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => Configuration.LoadImport(path, [spanish]))!;

        Assert.That(error.Message, Does.Contain("versión más reciente"));
    }

    [Test]
    public void LoadImport_RejectsDuplicateIdentitiesIgnoringCase()
    {
        string path = WriteJson("duplicates.json", """
        {
          "version": 3,
          "devices": [
            { "identity": "device:A", "detectedName": "A" },
            { "identity": "DEVICE:a", "detectedName": "A duplicado" }
          ],
          "mappings": [],
          "ignoredDeviceIds": []
        }
        """);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => Configuration.LoadImport(path, [spanish]))!;

        Assert.That(error.Message, Does.Contain("duplicado"));
    }

    [Test]
    public void LoadImport_RejectsDuplicateJsonPropertiesIgnoringCase()
    {
        string path = WriteJson("duplicate-properties.json", """
        { "version": 3, "devices": [], "Devices": [], "mappings": [], "ignoredDeviceIds": [] }
        """);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => Configuration.LoadImport(path, [spanish]))!;

        Assert.That(error.Message, Does.Contain("propiedad 'Devices' está duplicada"));
    }

    [Test]
    public void LoadImport_RejectsDanglingOrContradictoryReferences()
    {
        string dangling = WriteJson("dangling.json", """
        {
          "version": 3,
          "devices": [],
          "mappings": [{ "identity": "device:missing", "layout": "000000000000040A" }],
          "ignoredDeviceIds": []
        }
        """);
        string contradictory = WriteJson("contradictory.json", """
        {
          "version": 3,
          "devices": [{ "identity": "device:a", "detectedName": "A" }],
          "mappings": [{ "identity": "device:a", "layout": "000000000000040A" }],
          "ignoredDeviceIds": ["device:a"]
        }
        """);

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<InvalidDataException>(() => Configuration.LoadImport(dangling, [spanish]))!.Message,
                Does.Contain("ningún dispositivo"));
            Assert.That(
                Assert.Throws<InvalidDataException>(() => Configuration.LoadImport(contradictory, [spanish]))!.Message,
                Does.Contain("distribución y estar ignorado"));
        });
    }

    [Test]
    public void LoadImport_PreservesDeviceAndReportsUnavailableLayout()
    {
        string path = WriteJson("unavailable-layout.json", """
        {
          "version": 3,
          "devices": [{ "identity": "device:a", "detectedName": "A", "customName": "Alias" }],
          "mappings": [{ "identity": "device:a", "layout": "0000000000000409", "languageName": "inglés", "layoutName": "US" }],
          "ignoredDeviceIds": []
        }
        """);

        ConfigurationImportResult result = Configuration.LoadImport(path, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Configuration.Devices["device:a"].CustomName, Is.EqualTo("Alias"));
            Assert.That(result.Configuration.LayoutMappings, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LoadConfiguration_AcceptsNullCollectionsAsEmpty()
    {
        string path = WriteJson("null-collections.json", """
        { "version": 3, "devices": null, "mappings": null, "ignoredDeviceIds": null }
        """);

        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.That(loaded.Devices, Is.Empty);
    }

    [Test]
    public void ApplyImport_CreatesUniqueBackupsAndPersistsAtomically()
    {
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        string backups = Path.Combine(temporaryDirectory, "exports");
        Configuration current = new();
        current.SetLayout(Device("device:a", "MODEL-A", "A"), spanish);
        current.Save(path);
        Configuration imported = new();
        imported.Ignore(Device("device:b", "MODEL-B", "B"));

        string firstBackup = current.ApplyImport(imported, replace: true, path, backups);
        string secondBackup = current.CreateBackup(backups);
        Configuration persisted = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(firstBackup, Is.Not.EqualTo(secondBackup));
            Assert.That(File.Exists(firstBackup), Is.True);
            Assert.That(File.Exists(secondBackup), Is.True);
            Assert.That(current.Devices.Keys, Is.EquivalentTo(new[] { "device:b" }));
            Assert.That(persisted.IgnoredDevices.Contains("device:b"), Is.True);
        });
    }

    [Test]
    public void ApplyImport_WhenSaveFails_DoesNotMutateCurrentConfiguration()
    {
        string backups = Path.Combine(temporaryDirectory, "exports");
        Configuration current = new();
        current.SetLayout(Device("device:a", "MODEL-A", "A"), spanish);
        Configuration imported = new();
        imported.Ignore(Device("device:b", "MODEL-B", "B"));

        Assert.Catch<Exception>(() =>
            current.ApplyImport(imported, replace: true, temporaryDirectory, backups));

        Assert.Multiple(() =>
        {
            Assert.That(current.Devices.Keys, Is.EquivalentTo(new[] { "device:a" }));
            Assert.That(current.LayoutMappings.ContainsKey("device:a"), Is.True);
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

    [Test]
    public void Clear_WhenSaveFails_DoesNotMutateCurrentConfiguration()
    {
        Configuration configuration = new();
        configuration.SetLayout(Device("device:keyboard", "MODEL-A", "Teclado"), spanish);

        Assert.Catch<Exception>(() => configuration.Clear(temporaryDirectory));

        Assert.That(configuration.Devices.ContainsKey("device:keyboard"), Is.True);
    }

    [Test]
    public void Import_FromAnotherMachine_ResolvesAvailableLayoutsAndReportsMissing()
    {
        // AUT-12: importar preferencias de otro equipo con una distribución
        // ausente. Se resuelve lo posible, se conservan los dispositivos no
        // resueltos como pendientes, se informa lo no resuelto y se persiste sin
        // corrupción, con respaldo previo.
        string path = WriteJson("otro-equipo.json", """
        {
          "version": 3,
          "devices": [
            { "identity": "container:kbd-es", "fingerprint": "MODEL-ES", "detectedName": "Teclado ES", "customName": "Oficina" },
            { "identity": "container:kbd-us", "fingerprint": "MODEL-US", "detectedName": "Teclado US" }
          ],
          "mappings": [
            { "identity": "container:kbd-es", "layout": "000000000000040A", "languageName": "español (Chile)", "layoutName": "Latinoamericano" },
            { "identity": "container:kbd-us", "layout": "0000000000000409", "languageName": "inglés", "layoutName": "US" }
          ],
          "ignoredDeviceIds": []
        }
        """);
        string preferences = Path.Combine(temporaryDirectory, "preferences.json");
        string backups = Path.Combine(temporaryDirectory, "exports");
        Configuration current = new();

        ConfigurationImportResult import = Configuration.LoadImport(path, [spanish]);
        string backup = current.ApplyImport(import.Configuration, replace: true, preferences, backups);
        Configuration persisted = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), preferences, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(import.Warnings, Has.Count.EqualTo(1));
            Assert.That(import.Warnings.Single(), Does.Contain("container:kbd-us"));
            Assert.That(persisted.LayoutMappings["container:kbd-es"].Identifier, Is.EqualTo(spanish.Identifier));
            Assert.That(persisted.Devices["container:kbd-es"].CustomName, Is.EqualTo("Oficina"));
            Assert.That(persisted.Devices.ContainsKey("container:kbd-us"), Is.True);
            Assert.That(persisted.LayoutMappings.ContainsKey("container:kbd-us"), Is.False);
            Assert.That(File.Exists(backup), Is.True);
        });
    }

    private static KeyboardDevice Device(
        string identity,
        string fingerprint,
        string displayName,
        bool nonKeyboard = false) =>
        new("test-path", 1, identity, fingerprint, displayName, "ID DE PRUEBA", nonKeyboard);

    private string WriteJson(string fileName, string json)
    {
        string path = Path.Combine(temporaryDirectory, fileName);
        File.WriteAllText(path, json);
        return path;
    }
}
