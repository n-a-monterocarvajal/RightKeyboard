namespace RightKeyboard;

/// <summary>
/// Contrato de códigos de salida del frontend WinUI que el residente lanza bajo
/// demanda (<c>RightKeyboard.WinUI.exe</c>). Es la señal fiable con la que el
/// núcleo decide si debe abrir el fallback WinForms: el frontend solo promete
/// <see cref="StartupFailure"/> cuando arrancó pero no llegó a ser usable, de
/// modo que la recuperación se activa con certeza y no por suposición.
/// </summary>
internal static class FrontendExitCodes
{
    /// <summary>
    /// El frontend alcanzó un estado interactivo (creó y activó su ventana) y
    /// terminó con normalidad. El residente no abre el fallback.
    /// </summary>
    internal const int Success = 0;

    /// <summary>
    /// El frontend arrancó pero no llegó a ser usable: se capturó una excepción
    /// antes de activar la ventana. Es un código intencionado y distinguible
    /// ('RK' en ASCII) para no confundirse con la salida normal ni con un cierre
    /// abrupto del runtime posterior a la interacción. El residente debe abrir el
    /// fallback WinForms para la misma superficie (Configuración o selector).
    /// </summary>
    internal const int StartupFailure = 0x52_4B;

    /// <summary>
    /// Decide si el residente debe abrir el fallback WinForms a partir del código
    /// de salida del frontend. Solo se recupera ante la señal explícita de fallo
    /// de arranque: una salida normal o un fallo posterior a que la ventana ya
    /// fuese usable no reabre el fallback, porque el usuario sí dispuso de una
    /// interfaz funcional.
    /// </summary>
    internal static bool ShouldFallBack(int exitCode) => exitCode == StartupFailure;
}
