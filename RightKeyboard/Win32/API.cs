using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace RightKeyboard.Win32;

internal static class API
{
    internal const int WmInput = 0x00FF;
    internal const int WmInputDeviceChange = 0x00FE;
    private const uint RidInput = 0x10000003;
    private const uint RidiDeviceName = 0x20000007;
    private const uint RimTypeKeyboard = 1;
    private const uint RidevInputSink = 0x00000100;
    private const uint RidevDeviceNotify = 0x00002000;
    private const uint WmInputLanguageChangeRequest = 0x0050;
    private const int KlNameLength = 9;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RawInputDeviceList
    {
        public nint Device;
        public uint Type;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        [In] RAWINPUTDEVICE[] devices,
        uint deviceCount,
        uint structureSize);

    [DllImport("user32.dll", EntryPoint = "GetRawInputData", SetLastError = true)]
    private static extern uint GetRawInputData(
        nint rawInput,
        uint command,
        out RAWINPUT data,
        ref uint size,
        uint headerSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(
        [Out] RawInputDeviceList[]? devices,
        ref uint deviceCount,
        uint structureSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetRawInputDeviceInfoW(
        nint device,
        uint command,
        StringBuilder? data,
        ref uint dataSize);

    [DllImport("user32.dll")]
    private static extern nint ActivateKeyboardLayout(nint layout, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetKeyboardLayoutList(int count, [Out] nint[]? layouts);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetKeyboardLayoutNameW(StringBuilder name);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, nint processId);

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessageW(nint window, uint message, nint wParam, nint lParam);

    internal static void RegisterKeyboardInput(nint target)
    {
        RAWINPUTDEVICE[] devices = [new(0x01, 0x06, RidevInputSink | RidevDeviceNotify, target)];
        if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "No se pudo registrar la entrada de teclado.");
        }
    }

    internal static bool TryReadKeyboardEvent(nint rawInputHandle, out RawKeyboardEvent keyboardEvent)
    {
        keyboardEvent = default;
        uint size = (uint)Marshal.SizeOf<RAWINPUT>();
        uint headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
        uint bytesRead = GetRawInputData(rawInputHandle, RidInput, out RAWINPUT input, ref size, headerSize);
        if (bytesRead == uint.MaxValue ||
            bytesRead < headerSize ||
            input.Header.Type != RimTypeKeyboard ||
            input.Header.Device == 0)
        {
            return false;
        }

        keyboardEvent = new RawKeyboardEvent(
            input.Header.Device,
            input.Data.Keyboard.VirtualKey,
            input.Data.Keyboard.MakeCode,
            input.Data.Keyboard.Flags,
            input.Data.Keyboard.Message);
        return true;
    }

    internal static IReadOnlyList<RawInputDeviceList> GetKeyboardDevices()
    {
        uint count = 0;
        uint structureSize = (uint)Marshal.SizeOf<RawInputDeviceList>();
        if (GetRawInputDeviceList(null, ref count, structureSize) == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        if (count == 0)
        {
            return [];
        }

        RawInputDeviceList[] devices = new RawInputDeviceList[count];
        uint actualCount = count;
        if (GetRawInputDeviceList(devices, ref actualCount, structureSize) == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        return devices.Take(checked((int)actualCount)).Where(device => device.Type == RimTypeKeyboard).ToArray();
    }

    internal static string GetRawInputDeviceName(nint device)
    {
        uint characterCount = 0;
        if (GetRawInputDeviceInfoW(device, RidiDeviceName, null, ref characterCount) == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        StringBuilder name = new(checked((int)characterCount));
        if (GetRawInputDeviceInfoW(device, RidiDeviceName, name, ref characterCount) == uint.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        return name.ToString();
    }

    internal static nint[] GetKeyboardLayouts()
    {
        int count = checked((int)GetKeyboardLayoutList(0, null));
        if (count == 0)
        {
            return [];
        }

        nint[] layouts = new nint[count];
        int actualCount = checked((int)GetKeyboardLayoutList(count, layouts));
        return layouts.Take(actualCount).ToArray();
    }

    internal static (string LanguageName, string LayoutName) GetKeyboardLayoutDescription(nint keyboardLayout)
    {
        ushort languageId = unchecked((ushort)keyboardLayout.ToInt64());
        string languageName;
        try
        {
            languageName = CultureInfo.GetCultureInfo(languageId).DisplayName;
        }
        catch (CultureNotFoundException)
        {
            languageName = $"Idioma 0x{languageId:X4}";
        }

        nint currentLayout = GetKeyboardLayout(0);
        try
        {
            if (currentLayout != keyboardLayout)
            {
                ActivateKeyboardLayout(keyboardLayout, 0);
            }

            StringBuilder identifier = new(KlNameLength);
            string layoutName = GetKeyboardLayoutNameW(identifier)
                ? ReadLayoutDisplayName(identifier.ToString())
                : $"Distribución 0x{keyboardLayout.ToInt64():X}";

            return (languageName, layoutName);
        }
        finally
        {
            if (currentLayout != 0 && currentLayout != keyboardLayout)
            {
                ActivateKeyboardLayout(currentLayout, 0);
            }
        }
    }

    internal static bool IsForegroundLayout(nint desiredLayout)
    {
        nint foreground = GetForegroundWindow();
        if (foreground == 0)
        {
            return false;
        }

        uint threadId = GetWindowThreadProcessId(foreground, 0);
        return GetKeyboardLayout(threadId) == desiredLayout;
    }

    internal static bool RequestForegroundLayout(nint desiredLayout)
    {
        nint foreground = GetForegroundWindow();
        return foreground != 0 && PostMessageW(foreground, WmInputLanguageChangeRequest, 0, desiredLayout);
    }

    private static string ReadLayoutDisplayName(string identifier)
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\{identifier}");

        return key?.GetValue("Layout Text") as string ?? $"Distribución {identifier}";
    }
}
