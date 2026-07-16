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
            Assert.That(File.ReadAllText(path), Does.Contain("\"version\": 4"));
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
    public void LoadImport_RejectsFutureSchemaWithoutReadingItAsCurrent()
    {
        string path = WriteJson("future.json", """
        { "version": 5, "devices": [], "mappings": [], "ignoredDeviceIds": [] }
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

    private const string BaseusSignature = "enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.101";

    [Test]
    public void Ignore_ManualWithEmptyFingerprint_RegistersSignature()
    {
        Configuration configuration = new();
        KeyboardDevice presenter = Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature);

        bool registered = configuration.Ignore(presenter, extendToSignature: true);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.True);
            Assert.That(configuration.IgnoredSignatures, Is.EquivalentTo(new[] { BaseusSignature }));
            Assert.That(configuration.Devices[presenter.Identity].Signature, Is.EqualTo(BaseusSignature));
        });
    }

    [Test]
    public void Ignore_WithFingerprint_DoesNotRegisterSignature()
    {
        Configuration configuration = new();
        KeyboardDevice keyboard = Device("device:kbd", "MODEL-A", "Teclado real", signature: BaseusSignature);

        bool registered = configuration.Ignore(keyboard, extendToSignature: true);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.False);
            Assert.That(configuration.IgnoredSignatures, Is.Empty);
        });
    }

    [Test]
    public void Ignore_WithoutExtension_DoesNotRegisterSignature()
    {
        // El auto-ignorado por clasificación de nombre no debe extender a la
        // firma: sería una señal débil automática.
        Configuration configuration = new();
        KeyboardDevice mouse = Device("device:mouse", "", "MX Master 3S", nonKeyboard: true, signature: BaseusSignature);

        bool registered = configuration.Ignore(mouse);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.False);
            Assert.That(configuration.IgnoredSignatures, Is.Empty);
        });
    }

    [Test]
    public void IsIgnored_NewIdentityWithIgnoredSignature_LearnsIdentity()
    {
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:old-port", "", "Teclado sin nombre", signature: BaseusSignature),
            "Presentador",
            extendToSignature: true);

        KeyboardDevice reconnected = Device("device:new-port", "", "Teclado sin nombre", signature: BaseusSignature);
        bool ignored = configuration.IsIgnored(
            reconnected,
            connectedDevicesWithSameFingerprint: 0,
            connectedDevicesWithSameSignature: 1,
            out IgnoreEvaluation evaluation);

        Assert.Multiple(() =>
        {
            Assert.That(ignored, Is.True);
            Assert.That(evaluation.Source, Is.EqualTo(IgnoreSource.Signature));
            Assert.That(evaluation.LearnedIdentity, Is.True);
            Assert.That(configuration.IgnoredDevices.Contains(reconnected.Identity), Is.True);
            Assert.That(configuration.Devices[reconnected.Identity].CustomName, Is.EqualTo("Presentador"));
        });
    }

    [TestCase(2, "", "varias_coincidencias_conectadas")]
    [TestCase(1, "MODEL-A", "huella_presente")]
    public void IsIgnored_IgnoredSignature_DoesNotApplyWhenAmbiguous(
        int connectedWithSameSignature,
        string fingerprint,
        string expectedReason)
    {
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:old-port", "", "Teclado sin nombre", signature: BaseusSignature),
            extendToSignature: true);

        KeyboardDevice candidate = Device("device:new-port", fingerprint, "Teclado sin nombre", signature: BaseusSignature);
        bool ignored = configuration.IsIgnored(
            candidate,
            connectedDevicesWithSameFingerprint: 0,
            connectedWithSameSignature,
            out IgnoreEvaluation evaluation);

        Assert.Multiple(() =>
        {
            Assert.That(ignored, Is.False);
            Assert.That(evaluation.BlockedSignatureReason, Is.EqualTo(expectedReason));
            Assert.That(configuration.IgnoredDevices.Contains(candidate.Identity), Is.False);
        });
    }

    [Test]
    public void IsIgnored_SignatureSharedWithMappedDevice_DoesNotApply()
    {
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature),
            extendToSignature: true);
        configuration.SetLayout(
            Device("device:mapped", "", "Teclado sin nombre", signature: BaseusSignature), spanish);
        // SetLayout retira la firma; se restaura el escenario de conflicto
        // reintroduciéndola con otro portador ignorado aún presente.
        configuration.IgnoredSignatures.Add(BaseusSignature);

        bool ignored = configuration.IsIgnored(
            Device("device:new-port", "", "Teclado sin nombre", signature: BaseusSignature),
            connectedDevicesWithSameFingerprint: 0,
            connectedDevicesWithSameSignature: 1,
            out IgnoreEvaluation evaluation);

        Assert.Multiple(() =>
        {
            Assert.That(ignored, Is.False);
            Assert.That(evaluation.BlockedSignatureReason, Is.EqualTo("firma_con_distribucion"));
        });
    }

    [Test]
    public void UpdatePreference_Unignoring_RemovesSignature()
    {
        Configuration configuration = new();
        KeyboardDevice presenter = Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature);
        configuration.Ignore(presenter, extendToSignature: true);

        configuration.UpdatePreference(presenter.Identity, null, null, ignored: false);

        Assert.That(configuration.IgnoredSignatures, Is.Empty);
    }

    [Test]
    public void UpdatePreference_RenamingNeverIgnoredDevice_KeepsRegisteredSignature()
    {
        // Renombrar un gemelo no ignorado del mismo modelo no debe desactivar
        // la regla de firma que registró el dispositivo ignorado.
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature),
            extendToSignature: true);
        KeyboardDevice twin = Device("device:twin", "", "Teclado sin nombre", signature: BaseusSignature);
        configuration.TouchDevice(twin);

        configuration.UpdatePreference(twin.Identity, "Presentador de repuesto", null, ignored: false);

        Assert.That(configuration.IgnoredSignatures, Is.EquivalentTo(new[] { BaseusSignature }));
    }

    [Test]
    public void UpdatePreference_AssigningLayoutToSignatureCarrier_RemovesSignature()
    {
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature),
            extendToSignature: true);
        KeyboardDevice twin = Device("device:twin", "", "Teclado sin nombre", signature: BaseusSignature);
        configuration.TouchDevice(twin);

        configuration.UpdatePreference(twin.Identity, null, spanish, ignored: false);

        Assert.That(configuration.IgnoredSignatures, Is.Empty);
    }

    [Test]
    public void Save_PrunesSignatureOrphanedByCarrierSignatureChange()
    {
        // Un identity estable (container:) puede cambiar de firma al cambiar de
        // puerto; la firma anterior queda sin portador y debe podarse también en
        // memoria al guardar, no solo en el archivo.
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        Configuration configuration = new();
        configuration.Ignore(
            Device("container:stable", "", "Teclado sin nombre", signature: BaseusSignature),
            extendToSignature: true);
        configuration.TouchDevice(
            Device("container:stable", "", "Teclado sin nombre", signature: "enum:HID|vid:2571|pid:4104|mi:01|col:02|caps:-"));

        configuration.Save(path);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.IgnoredSignatures, Is.Empty);
            Assert.That(File.ReadAllText(path), Does.Contain("\"ignoredSignatures\": []"));
        });
    }

    [Test]
    public void UpdatePreference_IgnoringConnectedWeakDevice_RegistersSignature()
    {
        Configuration configuration = new();
        KeyboardDevice presenter = Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature);
        configuration.TouchDevice(presenter);

        bool registered = configuration.UpdatePreference(
            presenter.Identity, null, null, ignored: true, connectedDevice: presenter);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.True);
            Assert.That(configuration.IgnoredSignatures, Is.EquivalentTo(new[] { BaseusSignature }));
        });
    }

    [Test]
    public void SetLayout_RemovesSignature()
    {
        Configuration configuration = new();
        KeyboardDevice presenter = Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature);
        configuration.Ignore(presenter, extendToSignature: true);

        configuration.SetLayout(presenter, spanish);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.IgnoredSignatures, Is.Empty);
            Assert.That(configuration.IgnoredDevices, Is.Empty);
        });
    }

    [Test]
    public void Forget_LastCarrier_RemovesSignature_ButKeepsItWithIgnoredTwin()
    {
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:port-a", "", "Teclado sin nombre", signature: BaseusSignature), extendToSignature: true);
        configuration.Ignore(
            Device("device:port-b", "", "Teclado sin nombre", signature: BaseusSignature), extendToSignature: true);

        configuration.Forget("device:port-a");
        Assert.That(configuration.IgnoredSignatures, Is.EquivalentTo(new[] { BaseusSignature }));

        configuration.Forget("device:port-b");
        Assert.That(configuration.IgnoredSignatures, Is.Empty);
    }

    [Test]
    public void SaveAndLoad_RoundTripsSignatures()
    {
        string path = Path.Combine(temporaryDirectory, "preferences.json");
        Configuration configuration = new();
        configuration.Ignore(
            Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature),
            "Presentador",
            extendToSignature: true);

        configuration.Save(path);
        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.IgnoredSignatures, Is.EquivalentTo(new[] { BaseusSignature }));
            Assert.That(loaded.Devices["device:presenter"].Signature, Is.EqualTo(BaseusSignature));
            Assert.That(File.ReadAllText(path), Does.Contain("\"ignoredSignatures\""));
        });
    }

    [Test]
    public void Version3File_LoadsWithoutSignaturesAndSavesAsVersion4()
    {
        string path = WriteJson("preferences-v3.json", """
        {
          "version": 3,
          "devices": [{ "identity": "device:a", "detectedName": "A" }],
          "mappings": [],
          "ignoredDeviceIds": ["device:a"]
        }
        """);

        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);
        loaded.Save(path);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.IgnoredSignatures, Is.Empty);
            Assert.That(loaded.Devices["device:a"].Signature, Is.Null);
            Assert.That(File.ReadAllText(path), Does.Contain("\"version\": 4"));
        });
    }

    [Test]
    public void LoadConfiguration_Version4AcceptsNullCollectionsAsEmpty()
    {
        string path = WriteJson("null-collections-v4.json", """
        { "version": 4, "devices": null, "mappings": null, "ignoredDeviceIds": null, "ignoredSignatures": null }
        """);

        Configuration loaded = Configuration.LoadConfiguration(new KeyboardDevicesCollection(), path, [spanish]);

        Assert.That(loaded.Devices, Is.Empty);
    }

    [Test]
    public void LoadImport_RejectsInvalidSignatures()
    {
        string orphan = WriteJson("orphan-signature.json", """
        {
          "version": 4,
          "devices": [],
          "mappings": [],
          "ignoredDeviceIds": [],
          "ignoredSignatures": ["enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.101"]
        }
        """);
        string duplicated = WriteJson("duplicated-signature.json", """
        {
          "version": 4,
          "devices": [{ "identity": "device:a", "detectedName": "A", "signature": "enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.101" }],
          "mappings": [],
          "ignoredDeviceIds": ["device:a"],
          "ignoredSignatures": [
            "enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.101",
            "enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.101"
          ]
        }
        """);
        string malformed = WriteJson("malformed-signature.json", """
        {
          "version": 4,
          "devices": [{ "identity": "device:a", "detectedName": "A", "signature": "vid=2571" }],
          "mappings": [],
          "ignoredDeviceIds": [],
          "ignoredSignatures": []
        }
        """);

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<InvalidDataException>(() => Configuration.LoadImport(orphan, [spanish]))!.Message,
                Does.Contain("ningún dispositivo ignorado"));
            Assert.That(
                Assert.Throws<InvalidDataException>(() => Configuration.LoadImport(duplicated, [spanish]))!.Message,
                Does.Contain("duplicada"));
            Assert.That(
                Assert.Throws<InvalidDataException>(() => Configuration.LoadImport(malformed, [spanish]))!.Message,
                Does.Contain("no es canónica"));
        });
    }

    [Test]
    public void MergeFrom_PrunesSignaturesWithoutIgnoredCarrier()
    {
        Configuration current = new();
        KeyboardDevice presenter = Device("device:presenter", "", "Teclado sin nombre", signature: BaseusSignature);
        current.Ignore(presenter, extendToSignature: true);

        // El import trae el mismo dispositivo sin ignorar: al combinar, la firma
        // pierde su único portador y debe podarse.
        Configuration imported = new();
        imported.TouchDevice(presenter);

        current.MergeFrom(imported, replace: false);

        Assert.Multiple(() =>
        {
            Assert.That(current.IgnoredDevices, Is.Empty);
            Assert.That(current.IgnoredSignatures, Is.Empty);
        });
    }

    private static KeyboardDevice Device(
        string identity,
        string fingerprint,
        string displayName,
        bool nonKeyboard = false,
        string? signature = null) =>
        new("test-path", 1, identity, fingerprint, displayName, "ID DE PRUEBA", nonKeyboard, null, signature);

    private string WriteJson(string fileName, string json)
    {
        string path = Path.Combine(temporaryDirectory, fileName);
        File.WriteAllText(path, json);
        return path;
    }
}
