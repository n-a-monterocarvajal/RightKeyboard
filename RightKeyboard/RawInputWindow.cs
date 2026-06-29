using RightKeyboard.Win32;

namespace RightKeyboard;

internal sealed class RawInputWindow : NativeWindow, IDisposable
{
    private static readonly nint MessageOnlyWindow = new(-3);
    private readonly System.Windows.Forms.Timer devicesChangedTimer = new() { Interval = 200 };

    public RawInputWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "RightKeyboard.RawInput",
            Parent = MessageOnlyWindow
        });

        devicesChangedTimer.Tick += OnDevicesChangedTimerTick;
        API.RegisterKeyboardInput(Handle);
    }

    public event Action<RawKeyboardEvent>? KeyboardInput;

    public event Action? DevicesChanged;

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == API.WmInput && API.TryReadKeyboardEvent(message.LParam, out RawKeyboardEvent keyboardEvent))
        {
            if (keyboardEvent.IsKeyDown)
            {
                KeyboardInput?.Invoke(keyboardEvent);
            }
        }
        else if (message.Msg == API.WmInputDeviceChange)
        {
            devicesChangedTimer.Stop();
            devicesChangedTimer.Start();
        }

        // DefWindowProc debe recibir WM_INPUT para que Windows libere sus recursos internos.
        base.WndProc(ref message);
    }

    public void Dispose()
    {
        devicesChangedTimer.Stop();
        devicesChangedTimer.Tick -= OnDevicesChangedTimerTick;
        devicesChangedTimer.Dispose();
        KeyboardInput = null;
        DevicesChanged = null;
        DestroyHandle();
    }

    private void OnDevicesChangedTimerTick(object? sender, EventArgs e)
    {
        devicesChangedTimer.Stop();
        DevicesChanged?.Invoke();
    }
}
