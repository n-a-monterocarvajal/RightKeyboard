using System.Collections;
using RightKeyboard.Win32;

namespace RightKeyboard;

public sealed class KeyboardDevicesCollection : IEnumerable<KeyboardDevice>
{
    private readonly DeviceIdentityResolver identityResolver = new();
    private readonly Dictionary<nint, KeyboardDevice> devicesByHandle = [];
    private readonly Dictionary<string, KeyboardDevice> devicesByPath = new(StringComparer.OrdinalIgnoreCase);

    public KeyboardDevicesCollection()
    {
        Refresh();
    }

    public KeyboardDevice GetDevice(nint deviceHandle)
    {
        if (devicesByHandle.TryGetValue(deviceHandle, out KeyboardDevice device))
        {
            return device;
        }

        string path = API.GetRawInputDeviceName(deviceHandle);
        device = CreateDevice(path, deviceHandle);
        devicesByHandle[deviceHandle] = device;
        devicesByPath[path] = device;
        return device;
    }

    public string GetIdentityFromLegacyName(string devicePath) =>
        devicesByPath.TryGetValue(devicePath, out KeyboardDevice device)
            ? device.Identity
            : identityResolver.Resolve(devicePath).Identity;

    public int CountConnectedWithFingerprint(string fingerprint) => string.IsNullOrEmpty(fingerprint)
        ? 0
        : devicesByHandle.Values
            .Where(device => device.Fingerprint == fingerprint)
            .Select(device => device.Identity)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    public void Refresh()
    {
        devicesByHandle.Clear();
        devicesByPath.Clear();

        foreach (API.RawInputDeviceList rawDevice in API.GetKeyboardDevices())
        {
            try
            {
                string path = API.GetRawInputDeviceName(rawDevice.Device);
                KeyboardDevice device = CreateDevice(path, rawDevice.Device);
                devicesByHandle[device.Handle] = device;
                devicesByPath[device.DevicePath] = device;
            }
            catch
            {
                // Un dispositivo puede desaparecer entre la enumeración y la consulta.
            }
        }
    }

    private KeyboardDevice CreateDevice(string path, nint handle)
    {
        DeviceIdentityResolver.DeviceDescriptor descriptor = identityResolver.Resolve(path);
        return new KeyboardDevice(
            path,
            handle,
            descriptor.Identity,
            descriptor.Fingerprint,
            descriptor.DisplayName,
            descriptor.TechnicalId,
            descriptor.IsClearlyNonKeyboard);
    }

    public IEnumerator<KeyboardDevice> GetEnumerator() => devicesByHandle.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
