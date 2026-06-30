using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace RightKeyboard;

internal enum FluentBackdropKind
{
    Main = 2,
    Transient = 3,
    Tabbed = 4
}

internal static class FluentBackdropPolicy
{
    private static readonly Version Windows11Build22000 = new(10, 0, 22000);
    private static readonly Version Windows11Build22621 = new(10, 0, 22621);

    public static bool CanUseRoundedFrame(Version windowsVersion) => windowsVersion >= Windows11Build22000;

    public static bool CanUseSystemBackdrop(Version windowsVersion, bool highContrast) =>
        !highContrast && windowsVersion >= Windows11Build22621;
}

internal static class FluentWindowStyler
{
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int SystemBackdropType = 38;
    private const int RoundCornerPreference = 2;

    public static bool Apply(nint handle, FluentBackdropKind backdrop, bool darkMode)
    {
        Version version = Environment.OSVersion.Version;
        if (!FluentBackdropPolicy.CanUseRoundedFrame(version))
        {
            return false;
        }

        int dark = darkMode ? 1 : 0;
        int corner = RoundCornerPreference;
        TrySetAttribute(handle, UseImmersiveDarkMode, dark);
        TrySetAttribute(handle, WindowCornerPreference, corner);

        if (!FluentBackdropPolicy.CanUseSystemBackdrop(version, SystemInformation.HighContrast))
        {
            return false;
        }

        return TrySetAttribute(handle, SystemBackdropType, (int)backdrop);
    }

    private static bool TrySetAttribute(nint handle, int attribute, int value)
    {
        try
        {
            return DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)) >= 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int valueSize);
}

internal abstract class FluentForm : Form
{
    private readonly FluentBackdropKind backdrop;
    private bool backdropApplied;

    protected FluentForm(FluentBackdropKind backdrop)
    {
        this.backdrop = backdrop;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Font = FluentTypography.CreateBodyFont();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyBackdrop();
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        ApplyBackdrop();
        Invalidate(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (!backdropApplied)
        {
            base.OnPaintBackground(e);
        }
    }

    private void ApplyBackdrop()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        backdropApplied = FluentWindowStyler.Apply(Handle, backdrop, BackColor.GetBrightness() < 0.5f);
    }
}

internal sealed class FluentContextMenuStrip : ContextMenuStrip
{
    private bool backdropApplied;

    public bool BackdropApplied => backdropApplied;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyBackdrop();
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        ApplyBackdrop();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (!backdropApplied)
        {
            base.OnPaintBackground(e);
        }
    }

    private void ApplyBackdrop()
    {
        if (IsHandleCreated)
        {
            backdropApplied = FluentWindowStyler.Apply(Handle, FluentBackdropKind.Transient, BackColor.GetBrightness() < 0.5f);
        }
    }
}

internal static class FluentTypography
{
    public static Font CreateBodyFont() => CreateFont("Segoe UI Variable Text", 9f, FontStyle.Regular);

    public static Font CreateTitleFont(float size) => CreateFont("Segoe UI Variable Display", size, FontStyle.Bold);

    public static Font CreateSemiboldFont(float size) => CreateFont("Segoe UI Variable Text", size, FontStyle.Bold);

    private static Font CreateFont(string familyName, float size, FontStyle style)
    {
        Font font = new(familyName, size, style, GraphicsUnit.Point);
        if (font.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
        {
            return font;
        }

        font.Dispose();
        FontFamily fallbackFamily = SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily;
        return new Font(fallbackFamily, size, style, GraphicsUnit.Point);
    }
}

internal class FluentPanel : Panel
{
    [DefaultValue(8)]
    public int CornerRadius { get; set; } = 8;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRoundedRegion();
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        UpdateRoundedRegion();
    }

    protected void UpdateRoundedRegion()
    {
        if (Width <= 1 || Height <= 1 || SystemInformation.HighContrast)
        {
            Region?.Dispose();
            Region = null;
            return;
        }

        int radius = Math.Max(2, (int)Math.Round(CornerRadius * DeviceDpi / 96f));
        using GraphicsPath path = CreateRoundedPath(ClientRectangle, radius);
        Region? previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        bounds.Width--;
        bounds.Height--;
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
}

internal sealed class FluentTableLayoutPanel : TableLayoutPanel
{
    [DefaultValue(8)]
    public int CornerRadius { get; set; } = 8;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRoundedRegion();
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        UpdateRoundedRegion();
    }

    private void UpdateRoundedRegion()
    {
        if (Width <= 1 || Height <= 1 || SystemInformation.HighContrast)
        {
            Region?.Dispose();
            Region = null;
            return;
        }

        using GraphicsPath path = new();
        int radius = Math.Max(2, (int)Math.Round(CornerRadius * DeviceDpi / 96f));
        int diameter = Math.Min(radius * 2, Math.Min(Width - 1, Height - 1));
        Rectangle arc = new(0, 0, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = Width - diameter - 1;
        path.AddArc(arc, 270, 90);
        arc.Y = Height - diameter - 1;
        path.AddArc(arc, 0, 90);
        arc.X = 0;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        Region? previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }
}
