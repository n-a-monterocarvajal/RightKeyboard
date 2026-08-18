namespace RightKeyboard;

internal static class SettingsPanelVisualContract
{
    internal const string ReloadAccessibleName = "Recargar";
    internal const string ReloadToolTip = "Recarga la lista de dispositivos detectados.";

    // Radios de Fluent leídos del tema en tiempo de ejecución. Estas constantes son el
    // valor de respaldo documentado cuando el recurso no se resuelve, no un radio propio:
    // los controles heredan ControlCornerRadius y las superficies OverlayCornerRadius.
    internal const double ControlCornerRadius = 4;
    internal const double OverlayCornerRadius = 8;

    // El template de CheckBox enlaza el rectángulo del glifo con CheckBox.CornerRadius,
    // comprobado sobre el árbol visual real en 1.5.9. Heredar el radio nativo basta.
    internal const double CheckBoxGlyphCornerRadius = ControlCornerRadius;

    internal const double ReloadButtonSize = 32;
    internal const double ReloadIconSize = 16;

    // La barra nativa de selección de ListViewItem se dibuja fuera del contenido. Sin un
    // margen izquierdo mayor que el resto, queda pegada al nombre y al estado de la fila.
    internal const double DeviceRowContentLeftPadding = 16;
    internal const double DeviceRowContentPadding = 10;
    internal const double DeviceRowContentVerticalPadding = 8;

    internal const string SettingsSubtitle =
        "Administra los dispositivos detectados. Asigna a los teclados la distribución deseada.";
    internal const string EditorSectionDescription =
        "Edita el grupo lógico o el teclado seleccionado. Las identidades técnicas agrupadas " +
        "permanecen visibles en la lista de dispositivos detectados.";
    internal const string AliasEditingHint =
        "La identificación automática se reanudará al dejar de escribir.";

    // Glifo Fluent «Info», en lugar del separador tipográfico que precedía al aviso.
    internal const string InformationGlyph = "";
}
