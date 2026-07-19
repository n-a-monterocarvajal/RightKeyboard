using System.Globalization;
using System.Text.Json;

namespace RightKeyboard;

public sealed class Configuration
{
    public const int CurrentSchemaVersion = 5;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public Dictionary<string, DevicePreference> Devices { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Layout> LayoutMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> IgnoredDevices { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Firmas HID parciales (canónicas) cuyos portadores fueron ignorados
    /// manualmente. Solo se aplican a dispositivos con huella vacía; el
    /// invariante persistido es que toda firma tiene al menos un dispositivo
    /// ignorado portador.
    /// </summary>
    public HashSet<string> IgnoredSignatures { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Preferencias lógicas creadas únicamente por una acción explícita del
    /// usuario. Cada identidad técnica puede pertenecer como máximo a un grupo.
    /// </summary>
    public Dictionary<string, LogicalDeviceGroup> DeviceGroups { get; } = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, string> GroupMembership { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static Configuration LoadConfiguration(
        KeyboardDevicesCollection devices,
        string? configurationFile = null,
        IEnumerable<Layout>? availableLayouts = null)
    {
        string path = configurationFile ?? GetConfigFilePath();
        Layout[] layouts = (availableLayouts ?? Layout.EnumerateLayouts()).ToArray();

        if (File.Exists(path))
        {
            return LoadJson(path, layouts, warnings: null);
        }

        if (configurationFile is null && File.Exists(GetLegacyConfigFilePath()))
        {
            Configuration migrated = LoadLegacy(devices, layouts, GetLegacyConfigFilePath());
            migrated.Save(path);
            return migrated;
        }

        return new Configuration();
    }

    public static ConfigurationImportResult LoadImport(
        string path,
        IEnumerable<Layout>? availableLayouts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Layout[] layouts = (availableLayouts ?? Layout.EnumerateLayouts()).ToArray();
        List<string> warnings = [];
        Configuration configuration = LoadJson(path, layouts, warnings);
        return new ConfigurationImportResult(configuration, warnings);
    }

    public DevicePreference TouchDevice(KeyboardDevice device, string? customName = null)
    {
        if (!Devices.TryGetValue(device.Identity, out DevicePreference? preference))
        {
            preference = new DevicePreference { Identity = device.Identity };
            Devices[device.Identity] = preference;
        }

        preference.Fingerprint = device.Fingerprint;
        preference.Signature = device.Signature;
        preference.DetectedName = device.DisplayName;
        preference.TechnicalId = device.TechnicalId;
        preference.LastSeenUtc = DateTimeOffset.UtcNow;
        if (customName is not null)
        {
            preference.CustomName = NormalizeCustomName(customName, device.DisplayName);
        }

        return preference;
    }

    public string GetDisplayName(KeyboardDevice device) => GetGroup(device.Identity)?.DisplayName ??
        (Devices.TryGetValue(device.Identity, out DevicePreference? preference)
            ? preference.DisplayName
            : BuildSuggestedName(device.DisplayName, device.TechnicalId));

    public LogicalDeviceGroup? GetGroup(string identity) =>
        GroupMembership.TryGetValue(identity, out string? groupId) && DeviceGroups.TryGetValue(groupId, out LogicalDeviceGroup? group)
            ? group
            : null;

    public bool TryGetEffectiveLayout(string identity, out Layout? layout)
    {
        LogicalDeviceGroup? group = GetGroup(identity);
        if (group is not null)
        {
            layout = group.Layout;
            return layout is not null;
        }

        return LayoutMappings.TryGetValue(identity, out layout);
    }

    public bool TryGetLayout(
        KeyboardDevice device,
        int connectedDevicesWithSameFingerprint,
        out Layout? layout,
        out bool learnedIdentity)
    {
        learnedIdentity = false;
        TouchDevice(device);
        if (TryGetEffectiveLayout(device.Identity, out layout))
        {
            return true;
        }

        if (string.IsNullOrEmpty(device.Fingerprint) || connectedDevicesWithSameFingerprint != 1)
        {
            return false;
        }

        (string Identity, Layout Layout)[] candidatesByIdentity = Devices.Values
            .Where(preference => preference.Fingerprint == device.Fingerprint &&
                                 TryGetEffectiveLayout(preference.Identity, out _))
            .Select(preference => (preference.Identity, GetEffectiveLayout(preference.Identity)!))
            .ToArray();
        Layout[] candidates = candidatesByIdentity
            .Select(candidate => candidate.Layout)
            .DistinctBy(candidate => candidate.Identifier)
            .ToArray();
        if (candidates.Length == 0 || candidates.Select(candidate => candidate.Identifier).Distinct().Count() != 1)
        {
            return false;
        }

        DevicePreference? previous = candidatesByIdentity.Select(candidate => Devices[candidate.Identity]).FirstOrDefault();
        string? recoveredName = previous is null ? null : GetGroup(previous.Identity)?.DisplayName ?? previous.CustomName;
        DevicePreference current = TouchDevice(device, recoveredName);
        current.CustomName = recoveredName;
        layout = candidates[0];
        LayoutMappings[device.Identity] = layout;
        learnedIdentity = true;
        return true;
    }

    private Layout? GetEffectiveLayout(string identity) =>
        TryGetEffectiveLayout(identity, out Layout? layout) ? layout : null;

    public bool IsIgnored(
        KeyboardDevice device,
        int connectedDevicesWithSameFingerprint,
        int connectedDevicesWithSameSignature,
        out IgnoreEvaluation evaluation)
    {
        TouchDevice(device);
        if (IgnoredDevices.Contains(device.Identity))
        {
            evaluation = new IgnoreEvaluation(IgnoreSource.Identity, false, null);
            return true;
        }

        string[] candidates = Devices.Values
            .Where(preference => preference.Fingerprint == device.Fingerprint && IgnoredDevices.Contains(preference.Identity))
            .Select(preference => preference.Identity)
            .ToArray();
        if (!string.IsNullOrEmpty(device.Fingerprint) && connectedDevicesWithSameFingerprint == 1 &&
            candidates.Length == 1 &&
            !Devices.Values.Any(preference =>
                preference.Fingerprint == device.Fingerprint && TryGetEffectiveLayout(preference.Identity, out _)))
        {
            LearnIgnoredIdentity(device, Devices[candidates[0]]);
            evaluation = new IgnoreEvaluation(IgnoreSource.Fingerprint, true, null);
            return true;
        }

        // Recuperación por firma HID parcial: solo para dispositivos débilmente
        // identificados (huella vacía) cuya firma quedó registrada por un
        // ignorado manual, y solo cuando la coincidencia es inequívoca.
        if (device.Signature is null || !IgnoredSignatures.Contains(device.Signature))
        {
            evaluation = new IgnoreEvaluation(IgnoreSource.None, false, null);
            return false;
        }

        string? blockedReason = null;
        if (!string.IsNullOrEmpty(device.Fingerprint))
        {
            blockedReason = "huella_presente";
        }
        else if (connectedDevicesWithSameSignature != 1)
        {
            blockedReason = "varias_coincidencias_conectadas";
        }
        else if (Devices.Values.Any(preference =>
                     string.Equals(preference.Signature, device.Signature, StringComparison.Ordinal) &&
                     TryGetEffectiveLayout(preference.Identity, out _)))
        {
            blockedReason = "firma_con_distribucion";
        }

        if (blockedReason is not null)
        {
            evaluation = new IgnoreEvaluation(IgnoreSource.None, false, blockedReason);
            return false;
        }

        DevicePreference? carrier = Devices.Values
            .Where(preference => string.Equals(preference.Signature, device.Signature, StringComparison.Ordinal) &&
                                 IgnoredDevices.Contains(preference.Identity))
            .OrderByDescending(preference => preference.LastSeenUtc)
            .FirstOrDefault();
        LearnIgnoredIdentity(device, carrier);
        evaluation = new IgnoreEvaluation(IgnoreSource.Signature, true, null);
        return true;
    }

    private void LearnIgnoredIdentity(KeyboardDevice device, DevicePreference? previous)
    {
        DevicePreference current = TouchDevice(device, previous?.CustomName);
        current.CustomName = previous?.CustomName;
        IgnoredDevices.Add(device.Identity);
    }

    public void SetLayout(KeyboardDevice device, Layout layout, string? customName = null)
    {
        LogicalDeviceGroup? existingGroup = GetGroup(device.Identity);
        DevicePreference preference = TouchDevice(device, existingGroup is null ? customName : null);
        IgnoredDevices.Remove(device.Identity);
        if (existingGroup is LogicalDeviceGroup group)
        {
            group.CustomName = NormalizeGroupName(customName, group.DisplayName);
            group.Layout = layout;
            foreach (string memberIdentity in group.MemberIdentities)
            {
                RemoveSignature(Devices[memberIdentity].Signature);
            }
        }
        else
        {
            LayoutMappings[device.Identity] = layout;
        }
        // TouchDevice acaba de sincronizar preference.Signature con el dispositivo.
        RemoveSignature(preference.Signature);
    }

    /// <summary>
    /// Ignora el dispositivo y, si <paramref name="extendToSignature"/> lo pide
    /// y es seguro (huella vacía y firma disponible), registra también su firma
    /// HID parcial. Devuelve si la firma quedó registrada.
    /// </summary>
    public bool Ignore(KeyboardDevice device, string? customName = null, bool extendToSignature = false)
    {
        Ungroup(device.Identity);
        TouchDevice(device, customName);
        LayoutMappings.Remove(device.Identity);
        IgnoredDevices.Add(device.Identity);
        if (extendToSignature && string.IsNullOrEmpty(device.Fingerprint) && device.Signature is not null)
        {
            IgnoredSignatures.Add(device.Signature);
            return true;
        }

        return false;
    }

    public void UpdatePreference(string identity, string? customName, Layout? layout, bool ignored) =>
        UpdatePreference(identity, customName, layout, ignored, connectedDevice: null);

    /// <summary>
    /// Variante para el camino IPC: si el dispositivo está conectado, ignorarlo
    /// puede extender la exclusión a su firma HID parcial cuando sea seguro.
    /// Devuelve si la firma quedó registrada.
    /// </summary>
    public bool UpdatePreference(
        string identity,
        string? customName,
        Layout? layout,
        bool ignored,
        KeyboardDevice? connectedDevice)
    {
        if (!Devices.TryGetValue(identity, out DevicePreference? preference))
        {
            throw new InvalidOperationException("El dispositivo ya no existe en las preferencias.");
        }

        bool wasIgnored = IgnoredDevices.Contains(identity);
        if (GetGroup(identity) is LogicalDeviceGroup group)
        {
            if (ignored)
            {
                throw new InvalidOperationException("Separa la identidad del grupo antes de ignorarla.");
            }

            group.CustomName = NormalizeGroupName(customName, group.DisplayName);
            group.Layout = layout;
            if (layout is not null)
            {
                foreach (string memberIdentity in group.MemberIdentities)
                {
                    RemoveSignature(Devices[memberIdentity].Signature);
                }
            }
            return false;
        }

        preference.CustomName = NormalizeCustomName(customName, preference.DetectedName);
        if (ignored)
        {
            IgnoredDevices.Add(identity);
            LayoutMappings.Remove(identity);
            if (connectedDevice is KeyboardDevice device &&
                string.IsNullOrEmpty(device.Fingerprint) && device.Signature is not null)
            {
                IgnoredSignatures.Add(device.Signature);
                return true;
            }

            return false;
        }

        IgnoredDevices.Remove(identity);
        if (layout is null)
        {
            LayoutMappings.Remove(identity);
        }
        else
        {
            LayoutMappings[identity] = layout;
        }

        // Reactivar a un portador ignorado desactiva la regla de firma completa
        // (es la forma de deshacerla desde la UI); asignar distribución también,
        // porque esa firma quedaría bloqueada por contradicción de todos modos.
        // Renombrar un dispositivo que nunca estuvo ignorado no debe tocarla.
        if (wasIgnored || layout is not null)
        {
            RemoveSignature(preference.Signature);
        }

        return false;
    }

    public void Forget(string identity)
    {
        Ungroup(identity);
        Devices.Remove(identity, out DevicePreference? preference);
        LayoutMappings.Remove(identity);
        IgnoredDevices.Remove(identity);
        if (preference?.Signature is string signature && !HasIgnoredCarrier(signature))
        {
            IgnoredSignatures.Remove(signature);
        }
    }

    public string GroupDevices(
        string governingIdentity,
        string memberIdentity,
        string? customName = null,
        Layout? layoutOverride = null,
        bool overridePreference = false)
    {
        if (string.Equals(governingIdentity, memberIdentity, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Selecciona dos identidades distintas.");
        }

        if (!Devices.TryGetValue(governingIdentity, out DevicePreference? governingPreference) ||
            !Devices.ContainsKey(memberIdentity))
        {
            throw new InvalidOperationException("Una de las identidades ya no existe.");
        }

        LogicalDeviceGroup? governingGroup = GetGroup(governingIdentity);
        LogicalDeviceGroup? memberGroup = GetGroup(memberIdentity);
        if (memberGroup is not null)
        {
            throw new InvalidOperationException("La identidad seleccionada ya pertenece a un grupo.");
        }
        string currentDisplayName = governingGroup?.DisplayName ?? governingPreference.DisplayName;
        string displayName = overridePreference
            ? NormalizeGroupName(customName, currentDisplayName)
            : currentDisplayName;
        Layout? layout = overridePreference
            ? layoutOverride
            : governingGroup?.Layout ?? GetEffectiveLayout(governingIdentity);
        string groupId = governingGroup?.Id ?? $"group:{Guid.NewGuid():N}";
        HashSet<string> members = new(StringComparer.OrdinalIgnoreCase) { governingIdentity, memberIdentity };
        if (governingGroup is not null)
        {
            members.UnionWith(governingGroup.MemberIdentities);
        }

        if (members.Any(IgnoredDevices.Contains))
        {
            throw new InvalidOperationException("Reactiva las identidades ignoradas antes de agruparlas.");
        }

        if (governingGroup is not null)
        {
            RemoveGroup(governingGroup.Id);
        }
        LogicalDeviceGroup group = new(groupId, displayName, layout, members);
        DeviceGroups[groupId] = group;
        foreach (string identity in members)
        {
            GroupMembership[identity] = groupId;
            if (layout is not null)
            {
                RemoveSignature(Devices[identity].Signature);
            }
        }

        return groupId;
    }

    public void Ungroup(string identity)
    {
        LogicalDeviceGroup? group = GetGroup(identity);
        if (group is null)
        {
            return;
        }

        group.MemberIdentities.Remove(identity);
        GroupMembership.Remove(identity);
        if (group.MemberIdentities.Count == 1)
        {
            string remainingIdentity = group.MemberIdentities.Single();
            GroupMembership.Remove(remainingIdentity);
            DeviceGroups.Remove(group.Id);
        }
    }

    private void RemoveGroup(string groupId)
    {
        if (!DeviceGroups.Remove(groupId, out LogicalDeviceGroup? group))
        {
            return;
        }

        foreach (string identity in group.MemberIdentities)
        {
            GroupMembership.Remove(identity);
        }
    }

    private void RemoveSignature(string? signature)
    {
        if (signature is not null)
        {
            IgnoredSignatures.Remove(signature);
        }
    }

    private bool HasIgnoredCarrier(string signature) => Devices.Values.Any(preference =>
        string.Equals(preference.Signature, signature, StringComparison.Ordinal) &&
        IgnoredDevices.Contains(preference.Identity));

    public void MergeFrom(Configuration imported, bool replace)
    {
        ArgumentNullException.ThrowIfNull(imported);
        imported.ValidateState();

        if (replace)
        {
            Devices.Clear();
            LayoutMappings.Clear();
            IgnoredDevices.Clear();
            IgnoredSignatures.Clear();
            DeviceGroups.Clear();
            GroupMembership.Clear();
        }

        foreach ((string identity, DevicePreference preference) in imported.Devices)
        {
            Ungroup(identity);
            Devices[identity] = preference.Clone();
            LayoutMappings.Remove(identity);
            IgnoredDevices.Remove(identity);
        }

        foreach ((string identity, Layout layout) in imported.LayoutMappings)
        {
            LayoutMappings[identity] = layout;
            IgnoredDevices.Remove(identity);
        }

        foreach (string identity in imported.IgnoredDevices)
        {
            IgnoredDevices.Add(identity);
            LayoutMappings.Remove(identity);
        }

        IgnoredSignatures.UnionWith(imported.IgnoredSignatures);
        foreach (LogicalDeviceGroup importedGroup in imported.DeviceGroups.Values)
        {
            foreach (string identity in importedGroup.MemberIdentities)
            {
                Ungroup(identity);
            }

            string groupId = DeviceGroups.ContainsKey(importedGroup.Id)
                ? $"group:{Guid.NewGuid():N}"
                : importedGroup.Id;
            LogicalDeviceGroup group = importedGroup.Clone(groupId);
            DeviceGroups[groupId] = group;
            foreach (string identity in group.MemberIdentities)
            {
                GroupMembership[identity] = groupId;
                IgnoredDevices.Remove(identity);
            }
        }
        PruneOrphanSignatures();
    }

    /// <summary>
    /// Mantiene el invariante de que toda firma ignorada tiene al menos un
    /// dispositivo ignorado portador; una firma huérfana suprimiría entradas
    /// sin forma de deshacerlo desde la interfaz.
    /// </summary>
    private void PruneOrphanSignatures() =>
        IgnoredSignatures.RemoveWhere(signature => !HasIgnoredCarrier(signature));

    public void Save(string? configurationFile = null) =>
        SaveToPath(configurationFile ?? GetConfigFilePath());

    public void Export(string path) => SaveToPath(path);

    public string ApplyImport(
        Configuration imported,
        bool replace,
        string? configurationFile = null,
        string? backupDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(imported);
        Configuration candidate = Clone();
        candidate.MergeFrom(imported, replace);

        string backupPath = CreateBackup(backupDirectory);
        candidate.Save(configurationFile);
        ReplaceState(candidate);
        return backupPath;
    }

    public string CreateBackup(string? backupDirectory = null)
    {
        string directory = backupDirectory ?? GetExportDirectory();
        Directory.CreateDirectory(directory);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss-fff", CultureInfo.InvariantCulture);
        string path = Path.Combine(directory, $"RightKeyboard-respaldo-{timestamp}.json");
        for (int suffix = 2; File.Exists(path); suffix++)
        {
            path = Path.Combine(directory, $"RightKeyboard-respaldo-{timestamp}-{suffix}.json");
        }

        Export(path);
        return path;
    }

    public void Clear(string? configurationFile = null)
    {
        Configuration empty = new();
        empty.Save(configurationFile);
        ReplaceState(empty);
    }

    public static string GetConfigFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "preferences.json");

    public static string GetLegacyConfigFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "config.txt");

    public static string GetExportDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightKeyboard",
        "exports");

    private void SaveToPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ValidateState();
        // Restaurar el invariante también en memoria (una firma puede quedar
        // huérfana si TouchDevice actualizó la firma de su único portador);
        // así memoria y disco no divergen y la huérfana deja de suprimir.
        PruneOrphanSignatures();
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        StoredConfigurationV5 stored = new()
        {
            Devices = Devices.Values
                .OrderBy(preference => preference.DisplayName)
                .ThenBy(preference => preference.Identity)
                .Select(preference => StoredDevice.From(preference))
                .ToList(),
            Mappings = LayoutMappings
                .OrderBy(pair => pair.Key)
                .Select(pair => StoredMappingV3.From(pair.Key, pair.Value))
                .ToList(),
            IgnoredDeviceIds = IgnoredDevices.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            IgnoredSignatures = IgnoredSignatures.Order(StringComparer.Ordinal).ToList(),
            Groups = DeviceGroups.Values
                .OrderBy(group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(group => group.Id, StringComparer.OrdinalIgnoreCase)
                .Select(StoredDeviceGroup.From)
                .ToList()
        };

        string temporaryPath = fullPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(stored, JsonOptions));
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static Configuration LoadJson(
        string path,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        string json = File.ReadAllText(path);
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw InvalidData("La raíz debe ser un objeto JSON.");
        }

        ValidateNoDuplicateProperties(document.RootElement, "$");

        JsonElement? versionElement = null;
        foreach (JsonProperty property in document.RootElement.EnumerateObject()
                     .Where(property => string.Equals(property.Name, "version", StringComparison.OrdinalIgnoreCase)))
        {
            if (versionElement is not null)
            {
                throw InvalidData("La propiedad 'version' está duplicada.");
            }

            versionElement = property.Value;
        }

        if (versionElement is null &&
            document.RootElement.EnumerateObject().Any(property =>
                string.Equals(property.Name, "devices", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "ignoredDeviceIds", StringComparison.OrdinalIgnoreCase)))
        {
            throw InvalidData("Falta la propiedad 'version' para un documento con estructura de esquema 3.");
        }

        int version = versionElement is JsonElement value ? ReadVersion(value) : 2;
        return version switch
        {
            2 => LoadV2(json, layouts, warnings),
            3 => LoadV3(json, layouts, warnings),
            4 => LoadV4(json, layouts, warnings),
            CurrentSchemaVersion => LoadV5(json, layouts, warnings),
            > CurrentSchemaVersion => throw InvalidData(
                $"El esquema {version} fue creado por una versión más reciente de RightKeyboard. " +
                $"Esta versión admite hasta el esquema {CurrentSchemaVersion}."),
            _ => throw InvalidData($"El esquema {version} no es compatible.")
        };
    }

    private static Configuration LoadV3(
        string json,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        StoredConfigurationV5 stored = DeserializeStored(json);
        // El esquema 3 no porta firmas ni grupos; se migra en memoria y el
        // siguiente guardado escribe el esquema vigente.
        stored.IgnoredSignatures = [];
        stored.Groups = [];
        foreach (StoredDevice? device in stored.Devices)
        {
            if (device is not null)
            {
                device.Signature = null;
            }
        }

        return BuildFromStored(stored, layouts, warnings);
    }

    private static Configuration LoadV4(
        string json,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        StoredConfigurationV5 stored = DeserializeStored(json);
        stored.Groups = [];
        return BuildFromStored(stored, layouts, warnings);
    }

    private static Configuration LoadV5(
        string json,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings) => BuildFromStored(DeserializeStored(json), layouts, warnings);

    private static StoredConfigurationV5 DeserializeStored(string json)
    {
        StoredConfigurationV5 stored = JsonSerializer.Deserialize<StoredConfigurationV5>(json, JsonOptions)
            ?? throw InvalidData("El documento JSON no contiene preferencias.");
        stored.Devices ??= [];
        stored.Mappings ??= [];
        stored.IgnoredDeviceIds ??= [];
        stored.IgnoredSignatures ??= [];
        stored.Groups ??= [];
        return stored;
    }

    private static Configuration BuildFromStored(
        StoredConfigurationV5 stored,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        Configuration configuration = new();
        HashSet<string> deviceIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (StoredDevice? device in stored.Devices)
        {
            if (device is null)
            {
                throw InvalidData("La colección 'devices' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(device.Identity, "devices");
            if (!deviceIds.Add(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' está duplicado.");
            }

            DevicePreference preference = device.ToPreference(identity);
            preference.Signature = device.Signature is null
                ? null
                : RequireCanonicalSignature(device.Signature, $"devices ('{identity}')");
            configuration.Devices[identity] = preference;
        }

        HashSet<string> mappingIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (StoredMappingV3? mapping in stored.Mappings)
        {
            if (mapping is null)
            {
                throw InvalidData("La colección 'mappings' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(mapping.Identity, "mappings");
            if (!mappingIds.Add(identity))
            {
                throw InvalidData($"La asociación de '{identity}' está duplicada.");
            }

            if (!configuration.Devices.ContainsKey(identity))
            {
                throw InvalidData($"La asociación de '{identity}' no corresponde a ningún dispositivo.");
            }

            if (TryReadLayout(mapping.Layout, mapping.LanguageName, mapping.LayoutName, layouts, out Layout? layout))
            {
                configuration.LayoutMappings[identity] = layout!;
            }
            else
            {
                warnings?.Add($"No se encontró la distribución de '{identity}'; el dispositivo se importará sin asociación.");
            }
        }

        HashSet<string> ignoredIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (string storedIdentity in stored.IgnoredDeviceIds)
        {
            string identity = RequireIdentity(storedIdentity, "ignoredDeviceIds");
            if (!ignoredIds.Add(identity))
            {
                throw InvalidData($"El dispositivo ignorado '{identity}' está duplicado.");
            }

            if (!configuration.Devices.ContainsKey(identity))
            {
                throw InvalidData($"El dispositivo ignorado '{identity}' no existe en devices.");
            }

            if (configuration.LayoutMappings.ContainsKey(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' no puede tener distribución y estar ignorado a la vez.");
            }

            configuration.IgnoredDevices.Add(identity);
        }

        foreach (string storedSignature in stored.IgnoredSignatures)
        {
            string signature = RequireCanonicalSignature(storedSignature, "ignoredSignatures");
            if (!configuration.IgnoredSignatures.Add(signature))
            {
                throw InvalidData($"La firma ignorada '{signature}' está duplicada.");
            }

            if (!configuration.HasIgnoredCarrier(signature))
            {
                throw InvalidData(
                    $"La firma ignorada '{signature}' no corresponde a ningún dispositivo ignorado.");
            }
        }

        HashSet<string> groupIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> groupedIdentities = new(StringComparer.OrdinalIgnoreCase);
        foreach (StoredDeviceGroup? storedGroup in stored.Groups)
        {
            if (storedGroup is null)
            {
                throw InvalidData("La colección 'groups' contiene un elemento nulo.");
            }

            storedGroup.MemberIdentities ??= [];
            string groupId = RequireIdentity(storedGroup.Id, "groups");
            if (!groupIds.Add(groupId))
            {
                throw InvalidData($"El grupo '{groupId}' está duplicado.");
            }

            string[] members = storedGroup.MemberIdentities
                .Select(identity => RequireIdentity(identity, $"groups ('{groupId}')"))
                .ToArray();
            if (members.Length < 2 || members.Distinct(StringComparer.OrdinalIgnoreCase).Count() != members.Length)
            {
                throw InvalidData($"El grupo '{groupId}' debe contener al menos dos identidades distintas.");
            }

            foreach (string identity in members)
            {
                if (!configuration.Devices.ContainsKey(identity))
                {
                    throw InvalidData($"El miembro '{identity}' del grupo '{groupId}' no existe en devices.");
                }
                if (!groupedIdentities.Add(identity))
                {
                    throw InvalidData($"La identidad '{identity}' pertenece a más de un grupo.");
                }
                if (configuration.IgnoredDevices.Contains(identity))
                {
                    throw InvalidData($"El miembro '{identity}' no puede estar ignorado.");
                }
            }

            Layout? groupLayout = null;
            if (!string.IsNullOrEmpty(storedGroup.Layout) &&
                !TryReadLayout(storedGroup.Layout, storedGroup.LanguageName, storedGroup.LayoutName, layouts, out groupLayout))
            {
                warnings?.Add($"No se encontró la distribución del grupo '{groupId}'; se importará sin asociación.");
            }

            LogicalDeviceGroup group = new(groupId, storedGroup.DisplayName, groupLayout, members);
            configuration.DeviceGroups[groupId] = group;
            foreach (string identity in members)
            {
                configuration.GroupMembership[identity] = groupId;
            }
        }

        return configuration;
    }

    private static string RequireCanonicalSignature(string? value, string collection)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !HidSignature.TryParseCanonical(value, out HidSignature? signature) ||
            !string.Equals(signature!.ToCanonicalString(), value, StringComparison.Ordinal))
        {
            throw InvalidData($"La colección '{collection}' contiene una firma HID que no es canónica.");
        }

        return value;
    }

    private static Configuration LoadV2(
        string json,
        IReadOnlyList<Layout> layouts,
        List<string>? warnings)
    {
        StoredConfigurationV2 stored = JsonSerializer.Deserialize<StoredConfigurationV2>(json, JsonOptions)
            ?? throw InvalidData("El documento JSON no contiene preferencias.");
        stored.Mappings ??= [];
        stored.IgnoredDevices ??= [];
        Configuration configuration = new();
        HashSet<string> identities = new(StringComparer.OrdinalIgnoreCase);

        foreach (StoredMappingV2? mapping in stored.Mappings)
        {
            if (mapping is null)
            {
                throw InvalidData("La colección 'mappings' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(mapping.Identity, "mappings");
            if (!identities.Add(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' está duplicado en el esquema 2.");
            }

            configuration.Devices[identity] = new DevicePreference
            {
                Identity = identity,
                Fingerprint = mapping.Fingerprint ?? string.Empty,
                DetectedName = mapping.DisplayName ?? "Teclado",
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            if (TryReadLayout(mapping.Layout, null, null, layouts, out Layout? layout))
            {
                configuration.LayoutMappings[identity] = layout!;
            }
            else
            {
                warnings?.Add($"No se encontró la distribución de '{identity}'; el dispositivo se importará sin asociación.");
            }
        }

        foreach (StoredIgnoredDeviceV2? ignored in stored.IgnoredDevices)
        {
            if (ignored is null)
            {
                throw InvalidData("La colección 'ignoredDevices' contiene un elemento nulo.");
            }

            string identity = RequireIdentity(ignored.Identity, "ignoredDevices");
            if (!identities.Add(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' está duplicado en el esquema 2.");
            }

            configuration.Devices[identity] = new DevicePreference
            {
                Identity = identity,
                Fingerprint = ignored.Fingerprint ?? string.Empty,
                DetectedName = ignored.DisplayName ?? "Dispositivo ignorado",
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            configuration.IgnoredDevices.Add(identity);
        }

        return configuration;
    }

    private static Configuration LoadLegacy(
        KeyboardDevicesCollection devices,
        IReadOnlyList<Layout> layouts,
        string path)
    {
        Configuration configuration = new();
        foreach (string line in File.ReadLines(path))
        {
            int separator = line.LastIndexOf('=');
            if (separator <= 0 || separator == line.Length - 1 ||
                !TryReadLayout(line[(separator + 1)..], null, null, layouts, out Layout? layout))
            {
                continue;
            }

            string storedIdentity = line[..separator];
            string identity = storedIdentity.StartsWith("container:", StringComparison.OrdinalIgnoreCase) ||
                              storedIdentity.StartsWith("device:", StringComparison.OrdinalIgnoreCase) ||
                              storedIdentity.StartsWith("instance:", StringComparison.OrdinalIgnoreCase)
                ? storedIdentity
                : devices.GetIdentityFromLegacyName(storedIdentity);
            KeyboardDevice? connected = devices
                .Where(device => device.Identity == identity)
                .Select(device => (KeyboardDevice?)device)
                .FirstOrDefault();
            configuration.Devices[identity] = new DevicePreference
            {
                Identity = identity,
                Fingerprint = connected?.Fingerprint ?? string.Empty,
                DetectedName = connected?.DisplayName ?? "Teclado migrado",
                TechnicalId = connected?.TechnicalId ?? string.Empty,
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            configuration.LayoutMappings[identity] = layout!;
        }

        return configuration;
    }

    private Configuration Clone()
    {
        Configuration clone = new();
        clone.ReplaceState(this);
        return clone;
    }

    private void ReplaceState(Configuration source)
    {
        Devices.Clear();
        LayoutMappings.Clear();
        IgnoredDevices.Clear();
        IgnoredSignatures.Clear();
        DeviceGroups.Clear();
        GroupMembership.Clear();

        foreach ((string identity, DevicePreference preference) in source.Devices)
        {
            Devices[identity] = preference.Clone();
        }

        foreach ((string identity, Layout layout) in source.LayoutMappings)
        {
            LayoutMappings[identity] = layout;
        }

        IgnoredDevices.UnionWith(source.IgnoredDevices);
        IgnoredSignatures.UnionWith(source.IgnoredSignatures);
        foreach (LogicalDeviceGroup sourceGroup in source.DeviceGroups.Values)
        {
            LogicalDeviceGroup group = sourceGroup.Clone();
            DeviceGroups[group.Id] = group;
            foreach (string identity in group.MemberIdentities)
            {
                GroupMembership[identity] = group.Id;
            }
        }
    }

    private void ValidateState()
    {
        foreach ((string identity, DevicePreference preference) in Devices)
        {
            RequireIdentity(identity, "devices");
            if (!string.Equals(identity, preference.Identity, StringComparison.OrdinalIgnoreCase))
            {
                throw InvalidData($"La clave '{identity}' no coincide con la identidad del dispositivo.");
            }
        }

        foreach (string identity in LayoutMappings.Keys)
        {
            if (!Devices.ContainsKey(identity))
            {
                throw InvalidData($"La asociación de '{identity}' no corresponde a ningún dispositivo.");
            }

            if (IgnoredDevices.Contains(identity))
            {
                throw InvalidData($"El dispositivo '{identity}' no puede tener distribución y estar ignorado a la vez.");
            }
        }

        foreach (string identity in IgnoredDevices)
        {
            if (!Devices.ContainsKey(identity))
            {
                throw InvalidData($"El dispositivo ignorado '{identity}' no existe en devices.");
            }
        }

        HashSet<string> groupedIdentities = new(StringComparer.OrdinalIgnoreCase);
        foreach (LogicalDeviceGroup group in DeviceGroups.Values)
        {
            if (group.MemberIdentities.Count < 2)
            {
                throw InvalidData($"El grupo '{group.Id}' debe contener al menos dos identidades.");
            }

            foreach (string identity in group.MemberIdentities)
            {
                if (!Devices.ContainsKey(identity) || !groupedIdentities.Add(identity) || IgnoredDevices.Contains(identity))
                {
                    throw InvalidData($"La membresía de grupo de '{identity}' no es válida.");
                }

                if (!GroupMembership.TryGetValue(identity, out string? groupId) ||
                    !string.Equals(groupId, group.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw InvalidData($"El índice de membresía de '{identity}' no coincide con su grupo.");
                }
            }
        }

        if (GroupMembership.Count != groupedIdentities.Count)
        {
            throw InvalidData("El índice de membresía contiene identidades fuera de los grupos.");
        }
    }

    private static int ReadVersion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out int version))
        {
            throw InvalidData("La propiedad 'version' debe ser un número entero.");
        }

        return version;
    }

    private static void ValidateNoDuplicateProperties(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw InvalidData($"La propiedad '{property.Name}' está duplicada en {path}.");
                }

                ValidateNoDuplicateProperties(property.Value, $"{path}.{property.Name}");
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                ValidateNoDuplicateProperties(item, $"{path}[{index++}]");
            }
        }
    }

    private static string RequireIdentity(string? value, string collection)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw InvalidData($"La colección '{collection}' contiene una identidad vacía.");
        }

        string identity = value.Trim();
        if (!string.Equals(identity, value, StringComparison.Ordinal))
        {
            throw InvalidData($"La identidad '{value}' contiene espacios al principio o al final.");
        }

        return identity;
    }

    private static InvalidDataException InvalidData(string message) =>
        new($"preferences.json no es válido: {message}");

    private static bool TryReadLayout(
        string? identifierValue,
        string? languageName,
        string? layoutName,
        IReadOnlyList<Layout> layouts,
        out Layout? layout)
    {
        layout = null;
        if (ulong.TryParse(identifierValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong identifier))
        {
            layout = layouts.FirstOrDefault(candidate => candidate.Identifier == new nint(unchecked((long)identifier)));
        }

        layout ??= layouts.FirstOrDefault(candidate =>
            string.Equals(candidate.LanguageName, languageName, StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(candidate.LayoutName, layoutName, StringComparison.CurrentCultureIgnoreCase));
        return layout is not null;
    }

    private static string BuildSuggestedName(string detectedName, string technicalId) =>
        string.Equals(detectedName, "Teclado sin nombre", StringComparison.CurrentCultureIgnoreCase)
            ? $"Teclado {technicalId.Split(' ').LastOrDefault()}".Trim()
            : detectedName;

    private static string? NormalizeCustomName(string? customName, string detectedName)
    {
        string? normalized = string.IsNullOrWhiteSpace(customName) ? null : customName.Trim();
        return string.Equals(normalized, detectedName, StringComparison.CurrentCultureIgnoreCase) ? null : normalized;
    }

    private static string NormalizeGroupName(string? customName, string fallback)
    {
        string? normalized = string.IsNullOrWhiteSpace(customName) ? null : customName.Trim();
        return normalized ?? fallback;
    }

    private sealed class StoredConfigurationV5
    {
        public int Version { get; set; } = CurrentSchemaVersion;
        public List<StoredDevice> Devices { get; set; } = [];
        public List<StoredMappingV3> Mappings { get; set; } = [];
        public List<string> IgnoredDeviceIds { get; set; } = [];
        public List<string> IgnoredSignatures { get; set; } = [];
        public List<StoredDeviceGroup> Groups { get; set; } = [];
    }

    private sealed class StoredDeviceGroup
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Layout { get; set; }
        public string? LanguageName { get; set; }
        public string? LayoutName { get; set; }
        public List<string> MemberIdentities { get; set; } = [];

        public static StoredDeviceGroup From(LogicalDeviceGroup group) => new()
        {
            Id = group.Id,
            DisplayName = group.DisplayName,
            Layout = group.Layout is null
                ? null
                : unchecked((ulong)group.Layout.Identifier.ToInt64()).ToString("X16"),
            LanguageName = group.Layout?.LanguageName,
            LayoutName = group.Layout?.LayoutName,
            MemberIdentities = group.MemberIdentities.Order(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private sealed class StoredDevice
    {
        public string Identity { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
        public string? Signature { get; set; }
        public string DetectedName { get; set; } = string.Empty;
        public string? CustomName { get; set; }
        public string TechnicalId { get; set; } = string.Empty;
        public DateTimeOffset LastSeenUtc { get; set; }

        public static StoredDevice From(DevicePreference preference) => new()
        {
            Identity = preference.Identity,
            Fingerprint = preference.Fingerprint,
            Signature = preference.Signature,
            DetectedName = preference.DetectedName,
            CustomName = preference.CustomName,
            TechnicalId = preference.TechnicalId,
            LastSeenUtc = preference.LastSeenUtc
        };

        public DevicePreference ToPreference(string identity)
        {
            string detectedName = string.IsNullOrWhiteSpace(DetectedName) ? "Teclado" : DetectedName.Trim();
            return new DevicePreference
            {
                Identity = identity,
                Fingerprint = Fingerprint ?? string.Empty,
                DetectedName = detectedName,
                CustomName = NormalizeCustomName(CustomName, detectedName),
                TechnicalId = TechnicalId ?? string.Empty,
                LastSeenUtc = LastSeenUtc
            };
        }
    }

    private sealed class StoredMappingV3
    {
        public string Identity { get; set; } = string.Empty;
        public string Layout { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string LayoutName { get; set; } = string.Empty;

        public static StoredMappingV3 From(string identity, Layout layout) => new()
        {
            Identity = identity,
            Layout = unchecked((ulong)layout.Identifier.ToInt64()).ToString("X16"),
            LanguageName = layout.LanguageName,
            LayoutName = layout.LayoutName
        };
    }

    private sealed class StoredConfigurationV2
    {
        public List<StoredMappingV2> Mappings { get; set; } = [];
        public List<StoredIgnoredDeviceV2> IgnoredDevices { get; set; } = [];
    }

    private sealed class StoredMappingV2
    {
        public string Identity { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
        public string? DisplayName { get; set; }
        public string Layout { get; set; } = string.Empty;
    }

    private sealed class StoredIgnoredDeviceV2
    {
        public string Identity { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
        public string? DisplayName { get; set; }
    }
}

public sealed class DevicePreference
{
    public required string Identity { get; init; }
    public string Fingerprint { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string DetectedName { get; set; } = string.Empty;
    public string? CustomName { get; set; }
    public string TechnicalId { get; set; } = string.Empty;
    public DateTimeOffset LastSeenUtc { get; set; }
    public string DisplayName => string.IsNullOrWhiteSpace(CustomName) ? DetectedName : CustomName;

    public DevicePreference Clone() => new()
    {
        Identity = Identity,
        Fingerprint = Fingerprint,
        Signature = Signature,
        DetectedName = DetectedName,
        CustomName = CustomName,
        TechnicalId = TechnicalId,
        LastSeenUtc = LastSeenUtc
    };
}

public sealed class LogicalDeviceGroup
{
    public LogicalDeviceGroup(string id, string? displayName, Layout? layout, IEnumerable<string> memberIdentities)
    {
        Id = id;
        CustomName = string.IsNullOrWhiteSpace(displayName) ? "Teclado agrupado" : displayName.Trim();
        Layout = layout;
        MemberIdentities = new HashSet<string>(memberIdentities, StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }
    public string CustomName { get; set; }
    public string DisplayName => CustomName;
    public Layout? Layout { get; set; }
    public HashSet<string> MemberIdentities { get; }

    public LogicalDeviceGroup Clone(string? id = null) =>
        new(id ?? Id, CustomName, Layout, MemberIdentities);
}

public enum IgnoreSource
{
    None,
    Identity,
    Fingerprint,
    Signature
}

/// <summary>
/// Resultado estructurado de <see cref="Configuration.IsIgnored"/>. Cuando el
/// dispositivo portaba una firma registrada pero una condición la bloqueó,
/// <see cref="BlockedSignatureReason"/> explica cuál (para el diagnóstico).
/// </summary>
public sealed record IgnoreEvaluation(
    IgnoreSource Source,
    bool LearnedIdentity,
    string? BlockedSignatureReason);

public sealed record ConfigurationImportResult(
    Configuration Configuration,
    IReadOnlyList<string> Warnings);
