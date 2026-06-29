using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RightKeyboard;

internal sealed class DeviceIdentityResolver
{
    private readonly Dictionary<string, DeviceDescriptor> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, DeviceProperties>? propertyReader;

    public DeviceIdentityResolver()
    {
    }

    internal DeviceIdentityResolver(Func<string, DeviceProperties> propertyReader)
    {
        this.propertyReader = propertyReader;
    }

    public DeviceDescriptor Resolve(string devicePath)
    {
        if (cache.TryGetValue(devicePath, out DeviceDescriptor? descriptor))
        {
            return descriptor;
        }

        DeviceProperties properties = propertyReader?.Invoke(devicePath) ?? SetupApi.ReadDeviceProperties(devicePath);
        descriptor = BuildDescriptor(devicePath, properties);
        if (!properties.IsEmpty)
        {
            cache[devicePath] = descriptor;
        }

        return descriptor;
    }

    public void Refresh(IEnumerable<string> devicePaths)
    {
        string[] paths = devicePaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        IReadOnlyDictionary<string, DeviceProperties>? propertiesByPath = propertyReader is null
            ? SetupApi.ReadDeviceProperties(paths)
            : null;
        Dictionary<string, DeviceDescriptor> refreshed = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            DeviceProperties properties = propertiesByPath is null
                ? propertyReader!(path)
                : propertiesByPath[path];
            refreshed[path] = properties.IsEmpty &&
                              cache.TryGetValue(path, out DeviceDescriptor? previous) &&
                              !previous.Identity.StartsWith("device:", StringComparison.OrdinalIgnoreCase)
                ? previous
                : BuildDescriptor(path, properties);
        }

        cache.Clear();
        foreach ((string path, DeviceDescriptor descriptor) in refreshed)
        {
            cache[path] = descriptor;
        }
    }

    internal static DeviceDescriptor BuildDescriptor(string devicePath, DeviceProperties properties)
    {
        string displayName = BuildDisplayName(properties);
        string fingerprint = BuildFingerprint(properties, displayName);

        string identity;
        string technicalId;
        if (properties.ContainerId is Guid containerId && containerId != Guid.Empty)
        {
            identity = $"container:{containerId:D}";
            technicalId = $"Contenedor {containerId.ToString("N")[..8].ToUpperInvariant()}";
        }
        else if (!string.IsNullOrWhiteSpace(properties.InstanceId))
        {
            string hash = Hash(properties.InstanceId);
            identity = $"instance:{hash}";
            technicalId = $"Dispositivo {hash[..8]}";
        }
        else
        {
            string hash = Hash(devicePath);
            identity = $"device:{hash}";
            technicalId = $"Dispositivo {hash[..8]}";
        }

        return new DeviceDescriptor(
            identity,
            fingerprint,
            displayName,
            technicalId,
            DeviceClassifier.IsClearlyNonKeyboard(displayName));
    }

    private static string BuildDisplayName(DeviceProperties properties)
    {
        string name = FirstUsefulName(
            properties.BusReportedDescription,
            properties.FriendlyName,
            properties.Description) ?? "Teclado sin nombre";

        if (!string.IsNullOrWhiteSpace(properties.Manufacturer) &&
            !name.Contains(properties.Manufacturer, StringComparison.CurrentCultureIgnoreCase) &&
            !IsGenericManufacturer(properties.Manufacturer))
        {
            name = $"{properties.Manufacturer} {name}";
        }

        return name.Trim();
    }

    private static string BuildFingerprint(DeviceProperties properties, string displayName)
    {
        string hardware = string.Join('|', properties.HardwareIds.Order(StringComparer.OrdinalIgnoreCase));
        string material = $"{properties.Manufacturer}|{displayName}|{hardware}".Trim().ToUpperInvariant();
        return IsGenericName(displayName) && string.IsNullOrWhiteSpace(hardware)
            ? string.Empty
            : Hash(material);
    }

    private static string? FirstUsefulName(params string?[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate) && !IsGenericName(candidate));

    private static bool IsGenericName(string value) => GenericNames.Contains(value.Trim());

    private static readonly HashSet<string> GenericNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "HID Keyboard Device",
        "Dispositivo de teclado HID",
        "Standard PS/2 Keyboard",
        "Teclado estándar PS/2",
        "Teclado sin nombre"
    };

    private static bool IsGenericManufacturer(string value) =>
        value.Trim().Equals("(Standard keyboards)", StringComparison.OrdinalIgnoreCase) ||
        value.Trim().Equals("(Teclados estándar)", StringComparison.OrdinalIgnoreCase);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];

    internal sealed record DeviceDescriptor(
        string Identity,
        string Fingerprint,
        string DisplayName,
        string TechnicalId,
        bool IsClearlyNonKeyboard);

    internal sealed record DeviceProperties(
        Guid? ContainerId,
        string? InstanceId,
        string? BusReportedDescription,
        string? FriendlyName,
        string? Description,
        string? Manufacturer,
        IReadOnlyList<string> HardwareIds)
    {
        public static DeviceProperties Empty { get; } = new(null, null, null, null, null, null, []);

        public bool IsEmpty =>
            ContainerId is null &&
            string.IsNullOrWhiteSpace(InstanceId) &&
            string.IsNullOrWhiteSpace(BusReportedDescription) &&
            string.IsNullOrWhiteSpace(FriendlyName) &&
            string.IsNullOrWhiteSpace(Description) &&
            string.IsNullOrWhiteSpace(Manufacturer) &&
            HardwareIds.Count == 0;
    }

    private static class SetupApi
    {
        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfDeviceInterface = 0x00000010;
        private const uint SpdrpDeviceDescription = 0x00000000;
        private const uint SpdrpHardwareId = 0x00000001;
        private const uint SpdrpManufacturer = 0x0000000B;
        private const uint SpdrpFriendlyName = 0x0000000C;
        private static readonly nint InvalidHandleValue = new(-1);
        private static readonly Guid KeyboardInterfaceClass = new("884B96C3-56EF-11D1-BC8C-00A0C91405DD");
        private static readonly DevicePropertyKey ContainerIdKey = new(
            new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);
        private static readonly DevicePropertyKey BusReportedDescriptionKey = new(
            new Guid("540B947E-8B40-45BC-A8A2-6A0B894CBDA2"), 4);

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceInterfaceData
        {
            public uint Size;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public nint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceInfoData
        {
            public uint Size;
            public Guid ClassGuid;
            public uint DeviceInstance;
            public nint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct DevicePropertyKey
        {
            private readonly Guid formatId;
            private readonly uint propertyId;

            public DevicePropertyKey(Guid formatId, uint propertyId)
            {
                this.formatId = formatId;
                this.propertyId = propertyId;
            }
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint SetupDiGetClassDevsW(
            ref Guid classGuid,
            string? enumerator,
            nint parentWindow,
            uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            nint deviceInfoSet,
            nint deviceInfoData,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            ref DeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetailW(
            nint deviceInfoSet,
            ref DeviceInterfaceData deviceInterfaceData,
            nint deviceInterfaceDetailData,
            uint deviceInterfaceDetailDataSize,
            out uint requiredSize,
            ref DeviceInfoData deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDevicePropertyW(
            nint deviceInfoSet,
            ref DeviceInfoData deviceInfoData,
            ref DevicePropertyKey propertyKey,
            out uint propertyType,
            [Out] byte[]? propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize,
            uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryPropertyW(
            nint deviceInfoSet,
            ref DeviceInfoData deviceInfoData,
            uint property,
            out uint propertyType,
            [Out] byte[]? propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstanceIdW(
            nint deviceInfoSet,
            ref DeviceInfoData deviceInfoData,
            StringBuilder? deviceInstanceId,
            uint deviceInstanceIdSize,
            out uint requiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

        public static DeviceProperties ReadDeviceProperties(string expectedPath) =>
            ReadDeviceProperties([expectedPath])[expectedPath];

        public static IReadOnlyDictionary<string, DeviceProperties> ReadDeviceProperties(
            IReadOnlyCollection<string> expectedPaths)
        {
            Dictionary<string, DeviceProperties> result = expectedPaths.ToDictionary(
                path => path,
                _ => DeviceProperties.Empty,
                StringComparer.OrdinalIgnoreCase);
            if (result.Count == 0)
            {
                return result;
            }

            Guid interfaceClass = KeyboardInterfaceClass;
            nint deviceSet = SetupDiGetClassDevsW(
                ref interfaceClass,
                null,
                0,
                DigcfPresent | DigcfDeviceInterface);

            if (deviceSet == InvalidHandleValue)
            {
                return result;
            }

            try
            {
                for (uint index = 0; ; index++)
                {
                    DeviceInterfaceData interfaceData = new()
                    {
                        Size = (uint)Marshal.SizeOf<DeviceInterfaceData>()
                    };

                    if (!SetupDiEnumDeviceInterfaces(deviceSet, 0, ref interfaceClass, index, ref interfaceData))
                    {
                        return result;
                    }

                    DeviceInfoData deviceInfo = new()
                    {
                        Size = (uint)Marshal.SizeOf<DeviceInfoData>()
                    };

                    SetupDiGetDeviceInterfaceDetailW(
                        deviceSet,
                        ref interfaceData,
                        0,
                        0,
                        out uint requiredSize,
                        ref deviceInfo);

                    if (requiredSize == 0)
                    {
                        continue;
                    }

                    nint detailBuffer = Marshal.AllocHGlobal(checked((int)requiredSize));
                    try
                    {
                        Marshal.WriteInt32(detailBuffer, IntPtr.Size == 8 ? 8 : 6);
                        if (!SetupDiGetDeviceInterfaceDetailW(
                                deviceSet,
                                ref interfaceData,
                                detailBuffer,
                                requiredSize,
                                out _,
                                ref deviceInfo))
                        {
                            continue;
                        }

                        int pathOffset = IntPtr.Size == 8 ? 8 : 4;
                        string? actualPath = Marshal.PtrToStringUni(detailBuffer + pathOffset);
                        if (actualPath is null || !result.ContainsKey(actualPath))
                        {
                            continue;
                        }

                        result[actualPath] = new DeviceProperties(
                            ReadGuidProperty(deviceSet, ref deviceInfo, ContainerIdKey),
                            ReadInstanceId(deviceSet, ref deviceInfo),
                            ReadStringProperty(deviceSet, ref deviceInfo, BusReportedDescriptionKey),
                            ReadRegistryStrings(deviceSet, ref deviceInfo, SpdrpFriendlyName).FirstOrDefault(),
                            ReadRegistryStrings(deviceSet, ref deviceInfo, SpdrpDeviceDescription).FirstOrDefault(),
                            ReadRegistryStrings(deviceSet, ref deviceInfo, SpdrpManufacturer).FirstOrDefault(),
                            ReadRegistryStrings(deviceSet, ref deviceInfo, SpdrpHardwareId));

                        if (result.Values.All(properties => !properties.IsEmpty))
                        {
                            return result;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailBuffer);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceSet);
            }
        }

        private static Guid? ReadGuidProperty(
            nint deviceSet,
            ref DeviceInfoData deviceInfo,
            DevicePropertyKey key)
        {
            byte[] buffer = new byte[16];
            return SetupDiGetDevicePropertyW(
                deviceSet,
                ref deviceInfo,
                ref key,
                out _,
                buffer,
                (uint)buffer.Length,
                out _,
                0)
                ? new Guid(buffer)
                : null;
        }

        private static string? ReadStringProperty(
            nint deviceSet,
            ref DeviceInfoData deviceInfo,
            DevicePropertyKey key)
        {
            SetupDiGetDevicePropertyW(deviceSet, ref deviceInfo, ref key, out _, null, 0, out uint size, 0);
            if (size < sizeof(char))
            {
                return null;
            }

            byte[] buffer = new byte[size];
            return SetupDiGetDevicePropertyW(
                deviceSet,
                ref deviceInfo,
                ref key,
                out _,
                buffer,
                size,
                out _,
                0)
                ? Encoding.Unicode.GetString(buffer).TrimEnd('\0')
                : null;
        }

        private static IReadOnlyList<string> ReadRegistryStrings(
            nint deviceSet,
            ref DeviceInfoData deviceInfo,
            uint property)
        {
            SetupDiGetDeviceRegistryPropertyW(deviceSet, ref deviceInfo, property, out _, null, 0, out uint size);
            if (size < sizeof(char))
            {
                return [];
            }

            byte[] buffer = new byte[size];
            if (!SetupDiGetDeviceRegistryPropertyW(
                    deviceSet,
                    ref deviceInfo,
                    property,
                    out _,
                    buffer,
                    size,
                    out _))
            {
                return [];
            }

            return Encoding.Unicode.GetString(buffer)
                .Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static string? ReadInstanceId(nint deviceSet, ref DeviceInfoData deviceInfo)
        {
            SetupDiGetDeviceInstanceIdW(deviceSet, ref deviceInfo, null, 0, out uint size);
            if (size == 0)
            {
                return null;
            }

            StringBuilder value = new(checked((int)size));
            return SetupDiGetDeviceInstanceIdW(deviceSet, ref deviceInfo, value, size, out _)
                ? value.ToString()
                : null;
        }
    }
}
