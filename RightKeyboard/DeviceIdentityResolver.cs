using System.Runtime.InteropServices;

namespace RightKeyboard;

internal sealed class DeviceIdentityResolver
{
    private readonly Dictionary<string, string> cache = new(StringComparer.OrdinalIgnoreCase);

    public string Resolve(string devicePath)
    {
        if (cache.TryGetValue(devicePath, out string? identity))
        {
            return identity;
        }

        identity = SetupApi.TryGetContainerId(devicePath, out Guid containerId) && containerId != Guid.Empty
            ? $"container:{containerId:D}"
            : $"device:{devicePath}";

        cache[devicePath] = identity;
        return identity;
    }

    private static class SetupApi
    {
        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfDeviceInterface = 0x00000010;
        private static readonly nint InvalidHandleValue = new(-1);
        private static readonly Guid KeyboardInterfaceClass = new("884B96C3-56EF-11D1-BC8C-00A0C91405DD");
        private static readonly DevicePropertyKey ContainerIdKey = new(
            new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);

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
            [Out] byte[] propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize,
            uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

        public static bool TryGetContainerId(string expectedPath, out Guid containerId)
        {
            containerId = Guid.Empty;
            Guid interfaceClass = KeyboardInterfaceClass;
            nint deviceSet = SetupDiGetClassDevsW(
                ref interfaceClass,
                null,
                0,
                DigcfPresent | DigcfDeviceInterface);

            if (deviceSet == InvalidHandleValue)
            {
                return false;
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
                        return false;
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
                        if (!string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        byte[] value = new byte[16];
                        DevicePropertyKey propertyKey = ContainerIdKey;
                        if (!SetupDiGetDevicePropertyW(
                                deviceSet,
                                ref deviceInfo,
                                ref propertyKey,
                                out _,
                                value,
                                (uint)value.Length,
                                out _,
                                0))
                        {
                            return false;
                        }

                        containerId = new Guid(value);
                        return true;
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
    }
}
