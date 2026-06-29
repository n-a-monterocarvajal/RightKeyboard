namespace RightKeyboard;

internal sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    public ModernMenuRenderer() : base(new ModernColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
        {
            base.OnRenderMenuItemBackground(e);
            return;
        }

        Rectangle bounds = new(Point.Empty, e.Item.Size);
        bounds.Inflate(-4, -2);
        using SolidBrush brush = new(SystemColors.ControlLight);
        e.Graphics.FillRectangle(brush, bounds);
    }

    private sealed class ModernColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => SystemColors.Window;
        public override Color ImageMarginGradientBegin => SystemColors.Window;
        public override Color ImageMarginGradientMiddle => SystemColors.Window;
        public override Color ImageMarginGradientEnd => SystemColors.Window;
        public override Color MenuBorder => SystemColors.ControlDark;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => SystemColors.ControlLight;
        public override Color SeparatorDark => SystemColors.ControlLight;
        public override Color SeparatorLight => SystemColors.ControlLightLight;
    }
}
