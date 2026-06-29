using RightKeyboard.Win32;

namespace RightKeyboard;

internal sealed class RawInputWindow : NativeWindow, IDisposable
{
    private static readonly nint MessageOnlyWindow = new(-3);

    public RawInputWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "RightKeyboard.RawInput",
            Parent = MessageOnlyWindow
        });

        API.RegisterKeyboardInput(Handle);
    }

    public event Action<RawKeyboardEvent>? KeyboardInput;

    public event Action? DevicesChanged;

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == API.WmInput && API.TryReadKeyboardEvent(message.LParam, out RawKeyboardEvent keyboardEvent))
        {
            KeyboardInput?.Invoke(keyboardEvent);
        }
        else if (message.Msg == API.WmInputDeviceChange)
        {
            DevicesChanged?.Invoke();
        }

        // DefWindowProc debe recibir WM_INPUT para que Windows libere sus recursos internos.
        base.WndProc(ref message);
    }

    public void Dispose()
    {
        KeyboardInput = null;
        DevicesChanged = null;
        DestroyHandle();
    }
}
