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
        FluentPalette palette = FluentTheme.Current;
        using (SolidBrush background = new(palette.Surface))
        {
            e.Graphics.FillRectangle(background, new Rectangle(Point.Empty, e.Item.Size));
        }

        if (!e.Item.Selected)
        {
            return;
        }

        float scale = (e.ToolStrip?.DeviceDpi ?? 96) / 96f;
        int horizontalInset = (int)Math.Ceiling(4 * scale);
        int verticalInset = (int)Math.Ceiling(2 * scale);
        int availableWidth = e.ToolStrip?.DisplayRectangle.Width ?? e.Item.Width;
        Rectangle bounds = new(
            horizontalInset,
            verticalInset,
            Math.Max(1, availableWidth - (horizontalInset * 2)),
            Math.Max(1, e.Item.Height - (verticalInset * 2)));
        int radius = Math.Max(3, (int)Math.Round(6 * scale));
        Color selectionColor = SystemInformation.HighContrast
            ? SystemColors.Highlight
            : palette.Hover;

        SmoothingMode previousMode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = CreateRoundedRectangle(bounds, radius);
        using SolidBrush brush = new(selectionColor);
        e.Graphics.FillPath(brush, path);
        e.Graphics.SmoothingMode = previousMode;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using SolidBrush brush = new(FluentTheme.Current.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        FluentPalette palette = FluentTheme.Current;
        Color textColor = e.Item.Selected && SystemInformation.HighContrast
            ? SystemColors.HighlightText
            : palette.Text;
        Rectangle textBounds = new(
            12,
            0,
            Math.Max(1, (e.ToolStrip?.DisplayRectangle.Width ?? e.Item.Width) - 24),
            e.Item.Height);
        TextRenderer.DrawText(
            e.Graphics,
            e.Text,
            e.TextFont,
            textBounds,
            textColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        FluentPalette palette = FluentTheme.Current;
        using SolidBrush background = new(palette.Surface);
        e.Graphics.FillRectangle(background, e.Item.ContentRectangle);
        int y = e.Item.Height / 2;
        using Pen separator = new(palette.Border);
        e.Graphics.DrawLine(separator, 8, y, e.Item.Width - 8, y);
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
        public override Color ToolStripDropDownBackground => FluentTheme.Current.Surface;
        public override Color ImageMarginGradientBegin => FluentTheme.Current.Surface;
        public override Color ImageMarginGradientMiddle => FluentTheme.Current.Surface;
        public override Color ImageMarginGradientEnd => FluentTheme.Current.Surface;
        public override Color MenuBorder => SystemInformation.HighContrast ? SystemColors.WindowText : FluentTheme.Current.Border;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.Transparent;
        public override Color SeparatorDark => FluentTheme.Current.Border;
        public override Color SeparatorLight => FluentTheme.Current.Border;
    }
}
