using System.Runtime.InteropServices;

namespace RightKeyboard;

internal enum TrayMenuCommand : uint
{
    Settings = 1,
    Exit = 3
}

internal sealed class NativeTrayMenu : IDisposable
{
    private const uint MfString = 0;
    private const uint MfSeparator = 0x0800;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmNonotify = 0x0080;
    private const uint TpmReturnCommand = 0x0100;
    private readonly nint owner;
    private readonly nint handle;
    private readonly Action showSettings;
    private readonly Action exit;

    internal static IReadOnlyList<(TrayMenuCommand Command, string Text)> Commands { get; } =
    [
        (TrayMenuCommand.Settings, "Configuración"),
        (TrayMenuCommand.Exit, "Salir")
    ];

    internal NativeTrayMenu(nint owner, Action showSettings, Action exit)
    {
        this.owner = owner;
        this.showSettings = showSettings;
        this.exit = exit;
        handle = CreatePopupMenu();
        if (handle == 0)
        {
            throw new InvalidOperationException("Windows no pudo crear el menú de la bandeja.");
        }

        AppendMenuW(handle, MfString, (uint)TrayMenuCommand.Settings, "Configuración");
        AppendMenuW(handle, MfSeparator, 0, null);
        AppendMenuW(handle, MfString, (uint)TrayMenuCommand.Exit, "Salir");
    }

    internal void Show()
    {
        if (!GetCursorPos(out Point position))
        {
            return;
        }

        SetForegroundWindow(owner);
        TrayMenuCommand command = (TrayMenuCommand)TrackPopupMenuEx(
            handle,
            TpmRightButton | TpmNonotify | TpmReturnCommand,
            position.X,
            position.Y,
            owner,
            0);
        PostMessageW(owner, 0, 0, 0);

        switch (command)
        {
            case TrayMenuCommand.Settings: showSettings(); break;
            case TrayMenuCommand.Exit: exit(); break;
        }
    }

    public void Dispose() => DestroyMenu(handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenuW(nint menu, uint flags, uint item, string? text);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint window, nint parameters);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(nint window, uint message, nint wParam, nint lParam);
}
