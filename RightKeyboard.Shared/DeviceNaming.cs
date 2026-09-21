namespace RightKeyboard;

/// <summary>
/// Nombres de dispositivo compartidos por el núcleo y las dos interfaces.
/// </summary>
internal static class DeviceNaming
{
    /// <summary>
    /// Nombre mostrado cuando Windows no ofrece uno útil. Es deliberadamente
    /// neutro: ante un HID sin nombre RightKeyboard no puede saber si es un
    /// teclado, un mouse o un presentador —<c>DeviceClassifier</c> clasifica por
    /// el propio nombre—, de modo que no le atribuye una categoría que no ha
    /// comprobado.
    /// </summary>
    internal const string UnnamedDevice = "Dispositivo sin nombre";

    /// <summary>
    /// El mismo nombre hasta 1.6.1, cuando decía «Teclado». Se sigue
    /// reconociendo porque quedó guardado como <c>detectedName</c> en las
    /// preferencias de los usuarios, y sigue siendo el literal con el que se
    /// calcula la huella: ver <c>DeviceIdentityResolver.BuildFingerprint</c>.
    /// </summary>
    internal const string LegacyUnnamedDevice = "Teclado sin nombre";

    /// <summary>Nombre por omisión de un grupo lógico sin alias.</summary>
    internal const string UnnamedGroup = "Dispositivo agrupado";

    internal static bool IsUnnamed(string? detectedName) =>
        string.IsNullOrWhiteSpace(detectedName) ||
        string.Equals(detectedName.Trim(), UnnamedDevice, StringComparison.CurrentCultureIgnoreCase) ||
        string.Equals(detectedName.Trim(), LegacyUnnamedDevice, StringComparison.CurrentCultureIgnoreCase);

    /// <summary>
    /// Etiqueta con la que se identifica una identidad técnica: la fila
    /// subordinada de un grupo, o el nombre sugerido de un dispositivo que aún no
    /// tiene alias. Cuando el nombre detectado no identifica nada, el
    /// identificador técnico sí lo hace, y es además lo único que distingue dos
    /// identidades del mismo aparato conectado en puertos distintos.
    /// </summary>
    internal static string GetIdentityLabel(string? detectedName, string? technicalId) =>
        IsUnnamed(detectedName) && !string.IsNullOrWhiteSpace(technicalId)
            ? technicalId.Trim()
            : string.IsNullOrWhiteSpace(detectedName) ? UnnamedDevice : detectedName.Trim();

    /// <summary>
    /// Identificador técnico para acompañar a una etiqueta que ya nombra la fila
    /// por otro medio. Devuelve null cuando la etiqueta ya es ese identificador
    /// —repetirlo sería ruido— o cuando no hay identificador que mostrar. Sirve
    /// para que dos miembros de un grupo con el mismo nombre detectado, que es el
    /// caso del mismo modelo en dos puertos, sigan siendo distinguibles.
    /// </summary>
    internal static string? GetSecondaryTechnicalId(string? detectedName, string? technicalId) =>
        string.IsNullOrWhiteSpace(technicalId) || IsUnnamed(detectedName)
            ? null
            : technicalId.Trim();
}
