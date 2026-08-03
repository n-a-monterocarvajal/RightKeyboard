namespace RightKeyboard;

/// <summary>
/// Expone a ambos procesos si la compilación incluye diagnóstico detallado,
/// sin acoplar el frontend al logger ni a los modelos HID del residente.
/// </summary>
internal static class DiagnosticsAvailability
{
    internal static bool IsAvailable
    {
        get
        {
#if RIGHTKEYBOARD_DIAGNOSTICS
            return true;
#else
            return false;
#endif
        }
    }
}
