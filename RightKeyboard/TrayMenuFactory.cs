namespace RightKeyboard;

internal static class TrayMenuFactory
{
    public static ContextMenuStrip Create(
        Action showSettings,
        Action clearPreferences,
        Action exit)
    {
        FluentContextMenuStrip menu = new()
        {
            Renderer = new ModernMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(4),
            Font = FluentTypography.CreateBodyFont(),
            AccessibleName = "Menú de RightKeyboard"
        };

        menu.Items.Add(CreateItem(
            "&Configuración",
            "Abre la edición de dispositivos y preferencias.",
            showSettings));
        menu.Items.Add(CreateItem(
            "&Limpiar preferencias",
            "Elimina las preferencias guardadas después de pedir confirmación.",
            clearPreferences));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateItem(
            "&Salir",
            "Cierra RightKeyboard.",
            exit));
        return menu;
    }

    private static ToolStripMenuItem CreateItem(
        string text,
        string description,
        Action action)
    {
        ToolStripMenuItem item = new(text, null, (_, _) => action())
        {
            AutoSize = true,
            Padding = new Padding(12, 7, 16, 7),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoToolTip = false,
            AccessibleName = text.Replace("&", string.Empty, StringComparison.Ordinal),
            AccessibleDescription = description
        };
        return item;
    }
}
