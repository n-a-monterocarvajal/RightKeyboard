using System.Drawing.Drawing2D;

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

        float scale = (e.ToolStrip?.DeviceDpi ?? 96) / 96f;
        Rectangle bounds = new(Point.Empty, e.Item.Size);
        bounds.Inflate(-(int)Math.Ceiling(4 * scale), -(int)Math.Ceiling(2 * scale));
        int radius = Math.Max(3, (int)Math.Round(6 * scale));
        Color selectionColor = SystemInformation.HighContrast
            ? SystemColors.Highlight
            : Color.FromArgb(42, SystemColors.Highlight);

        SmoothingMode previousMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = CreateRoundedRectangle(bounds, radius);
        using SolidBrush brush = new(selectionColor);
        e.Graphics.FillPath(brush, path);
        e.Graphics.SmoothingMode = previousMode;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item.Selected && SystemInformation.HighContrast)
        {
            e.TextColor = SystemColors.HighlightText;
        }

        base.OnRenderItemText(e);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));
        GraphicsPath path = new();
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class ModernColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => SystemColors.Window;
        public override Color ImageMarginGradientBegin => SystemColors.Window;
        public override Color ImageMarginGradientMiddle => SystemColors.Window;
        public override Color ImageMarginGradientEnd => SystemColors.Window;
        public override Color MenuBorder => SystemInformation.HighContrast ? SystemColors.WindowText : SystemColors.ControlDark;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.Transparent;
        public override Color SeparatorDark => SystemColors.ControlLight;
        public override Color SeparatorLight => SystemColors.ControlLightLight;
    }
}
