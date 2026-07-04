using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.UI;

namespace RightKeyboard.WinUI;

public sealed class LayoutSelectionWindow : Window
{
    private readonly SettingsIpcClient client;
    private readonly string identity;
    private readonly TextBox alias = new();
    private readonly ListView layouts = new();
    private readonly Button accept = new();
    private SettingsDevice? device;

    public LayoutSelectionWindow(SettingsIpcClient client, string identity)
    {
        this.client = client;
        this.identity = identity;
        Title = "Seleccionar distribución del teclado";
        ExtendsContentIntoTitleBar = true;
        Content = BuildContent();
        TryEnableBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(720, 680));
        Activated += OnActivated;
    }

    private UIElement BuildContent()
    {
        Grid root = new() { Padding = new Thickness(24, 0, 24, 24), RowSpacing = 16 };
        root.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid titleBar = CreateTitleBar();
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        StackPanel heading = new() { Spacing = 4 };
        heading.Children.Add(new TextBlock
        {
            Text = "Configura este teclado",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Elige una distribución o ignora el dispositivo si no corresponde a un teclado.",
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetRow(heading, 1);
        root.Children.Add(heading);

        alias.Header = "Nombre para este teclado";
        alias.PlaceholderText = "Nombre reconocible";
        alias.CornerRadius = new CornerRadius(8);
        Grid.SetRow(alias, 2);
        root.Children.Add(alias);

        layouts.CornerRadius = new CornerRadius(10);
        layouts.SelectionChanged += (_, _) => accept.IsEnabled = layouts.SelectedItem is ListViewItem { Tag: SettingsLayout };
        Grid.SetRow(layouts, 3);
        root.Children.Add(layouts);

        Grid actions = new() { ColumnSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Button ignore = ActionButton("Ignorar este dispositivo");
        ignore.Click += Ignore_Click;
        actions.Children.Add(ignore);
        Button cancel = ActionButton("Cancelar");
        cancel.Click += (_, _) => Close();
        Grid.SetColumn(cancel, 1);
        actions.Children.Add(cancel);
        accept.Content = "Aceptar";
        accept.CornerRadius = new CornerRadius(8);
        accept.MinHeight = 36;
        accept.Padding = new Thickness(16, 6, 16, 6);
        accept.IsEnabled = false;
        accept.Click += Accept_Click;
        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out object style) && style is Style accentStyle)
        {
            accept.Style = accentStyle;
        }
        Grid.SetColumn(accept, 2);
        actions.Children.Add(accept);
        Grid.SetRow(actions, 4);
        root.Children.Add(actions);
        return root;
    }

    private Grid CreateTitleBar()
    {
        Grid bar = new() { Padding = new Thickness(0, 0, 150, 0), ColumnSpacing = 10 };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri("ms-appx:///Assets/RightKeyboard.ico")),
            Width = 20,
            Height = 20
        });
        TextBlock title = new()
        {
            Text = "RightKeyboard",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        Grid.SetColumn(title, 1);
        bar.Children.Add(title);
        return bar;
    }

    private static Button ActionButton(string text) => new()
    {
        Content = text,
        CornerRadius = new CornerRadius(8),
        MinHeight = 36,
        Padding = new Thickness(16, 6, 16, 6)
    };

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnActivated;
        double scale = (Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)Math.Ceiling(720 * scale),
            (int)Math.Ceiling(680 * scale)));
        SettingsSnapshot snapshot = await client.GetSnapshotAsync();
        device = snapshot.Devices.FirstOrDefault(candidate =>
            string.Equals(candidate.Identity, identity, StringComparison.OrdinalIgnoreCase));
        if (device is null)
        {
            Close();
            return;
        }

        alias.Text = device.DisplayName;
        string? currentLanguage = null;
        foreach (SettingsLayout layout in snapshot.Layouts)
        {
            if (!string.Equals(currentLanguage, layout.LanguageName, StringComparison.CurrentCultureIgnoreCase))
            {
                currentLanguage = layout.LanguageName;
                layouts.Items.Add(new ListViewItem
                {
                    Content = new TextBlock
                    {
                        Text = currentLanguage,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    IsHitTestVisible = false,
                    IsTabStop = false,
                    Margin = new Thickness(0, 8, 0, 2)
                });
            }

            ListViewItem item = new()
            {
                Tag = layout,
                Content = layout.LayoutName,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(12, 2, 0, 2),
                Padding = new Thickness(12, 8, 12, 8)
            };
            layouts.Items.Add(item);
            if (device.LayoutIdentifier == layout.Identifier)
            {
                layouts.SelectedItem = item;
            }
        }

        ActivateSelectorWindow();
        alias.Focus(FocusState.Programmatic);
    }

    private void ActivateSelectorWindow()
    {
        nint handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        AppWindow.Show(true);
        ShowWindow(handle, SwRestore);

        nint foreground = GetForegroundWindow();
        uint foregroundThread = foreground == 0 ? 0 : GetWindowThreadProcessId(foreground, out _);
        uint currentThread = GetCurrentThreadId();
        bool attached = foregroundThread != 0 && foregroundThread != currentThread &&
                        AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
            SetFocus(handle);
        }
        finally
        {
            if (attached) AttachThreadInput(currentThread, foregroundThread, false);
        }

        // Windows puede conservar el foco de la aplicación anterior. Este pulso
        // garantiza que el selector quede visible sin mantenerlo "siempre arriba".
        if (GetForegroundWindow() != handle)
        {
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNotTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }
    }

    private async void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (layouts.SelectedItem is not ListViewItem { Tag: SettingsLayout layout })
        {
            return;
        }

        await client.SaveAsync(identity, alias.Text, layout.Identifier, false);
        Close();
    }

    private async void Ignore_Click(object sender, RoutedEventArgs e)
    {
        await client.SaveAsync(identity, alias.Text, null, true);
        Close();
    }

    private void TryEnableBackdrop()
    {
        try { SystemBackdrop = new DesktopAcrylicBackdrop(); }
        catch
        {
            try { SystemBackdrop = new MicaBackdrop(); }
            catch { SystemBackdrop = null; }
        }
        AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(nint window);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attachThread, uint attachToThread, bool attach);

    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    private static readonly nint HwndTopmost = new(-1);
    private static readonly nint HwndNotTopmost = new(-2);
    private const int SwRestore = 9;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpShowWindow = 0x0040;
}
