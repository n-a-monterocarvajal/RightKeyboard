namespace RightKeyboard;

/// <summary>
/// Contrato de códigos de salida entre el frontend WinUI y el residente.
/// </summary>
internal static class FrontendExitCodes
{
    internal const int Success = 0;
    internal const int StartupFailure = 0x52_4B;

    internal static bool ShouldFallBack(int exitCode) => exitCode == StartupFailure;
}
