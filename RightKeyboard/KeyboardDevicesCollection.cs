using System.Collections;
using RightKeyboard.Win32;

namespace RightKeyboard;

public sealed class KeyboardDevicesCollection : IEnumerable<KeyboardDevice>
{
    private readonly DeviceIdentityResolver identityResolver = new();
    private readonly Dictionary<nint, KeyboardDevice> devicesByHandle = [];
    private readonly Dictionary<string, KeyboardDevice> devicesByName = new(StringComparer.OrdinalIgnoreCase);

    public KeyboardDevicesCollection()
    {
        Refresh();
    }

    public string GetIdentity(nint deviceHandle)
    {
        if (devicesByHandle.TryGetValue(deviceHandle, out KeyboardDevice device))
        {
            return device.Identity;
        }

        string name = API.GetRawInputDeviceName(deviceHandle);
        string identity = identityResolver.Resolve(name);
        device = new KeyboardDevice(name, deviceHandle, identity);
        devicesByHandle[deviceHandle] = device;
        devicesByName[name] = device;
        return identity;
    }

    public string GetIdentityFromLegacyName(string deviceName) =>
        devicesByName.TryGetValue(deviceName, out KeyboardDevice device)
            ? device.Identity
            : identityResolver.Resolve(deviceName);

    public void Refresh()
    {
        foreach (API.RawInputDeviceList rawDevice in API.GetKeyboardDevices())
        {
            try
            {
                string name = API.GetRawInputDeviceName(rawDevice.Device);
                string identity = identityResolver.Resolve(name);
                KeyboardDevice device = new(name, rawDevice.Device, identity);
                devicesByHandle[device.Handle] = device;
                devicesByName[device.Name] = device;
            }
            catch
            {
                // Un dispositivo puede desaparecer entre la enumeración y la consulta.
            }
        }
    }

    public IEnumerator<KeyboardDevice> GetEnumerator() => devicesByHandle.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
