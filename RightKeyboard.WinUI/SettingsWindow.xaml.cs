using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RightKeyboard.WinUI;

public sealed class SettingsWindow : Window
{
    private readonly SettingsIpcClient client;
    private readonly ObservableCollection<DeviceRow> rows = [];
    private readonly ListView DeviceList = new();
    private readonly TextBox AliasTextBox = new();
    private readonly TextBlock DetectedNameText = new();
    private readonly TextBlock TechnicalIdText = new();
    private readonly TextBlock StatusText = new();
    private readonly ComboBox LayoutComboBox = new();
    private readonly CheckBox IgnoredCheckBox = new();
    private readonly Button SaveButton = new();
    private readonly Button ForgetButton = new();
    private readonly List<Button> buttons = [];
    private readonly List<Border> cards = [];
    private readonly List<TextBlock> secondaryText = [];
    private Grid? contentRoot;
    private SettingsSnapshot? snapshot;

    public SettingsWindow(SettingsIpcClient client)
    {
        this.client = client;
        Title = "Configuración de RightKeyboard";
        ExtendsContentIntoTitleBar = true;
        Content = BuildContent();
        ConfigureCaptionButtons();
        ApplyFluentResources();
        TryEnableBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1020, 760));
        Activated += OnActivated;
    }

    private DeviceRow? SelectedRow => (DeviceList.SelectedItem as ListViewItem)?.Tag as DeviceRow;

    private UIElement BuildContent()
    {
        Grid root = new() { Padding = new Thickness(24, 0, 24, 24), RowSpacing = 16 };
        contentRoot = root;
        root.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        root.ActualThemeChanged += (_, _) => ApplyFluentResources();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid titleBar = new() { Padding = new Thickness(0, 0, 150, 0), ColumnSpacing = 10 };
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        FontIcon appIcon = new()
        {
            Glyph = "\uE765",
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 16
        };
        titleBar.Children.Add(appIcon);
        TextBlock appTitle = new()
        {
            Text = "RightKeyboard",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        Grid.SetColumn(appTitle, 1);
        titleBar.Children.Add(appTitle);
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        StackPanel heading = new() { Spacing = 4 };
        heading.Children.Add(new TextBlock
        {
            Text = "Teclados y preferencias",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        TextBlock subtitle = new()
        {
            Text = "Administra los teclados conocidos sin interrumpir la detección en segundo plano.",
            TextWrapping = TextWrapping.Wrap
        };
        secondaryText.Add(subtitle);
        heading.Children.Add(subtitle);
        Grid.SetRow(heading, 1);
        root.Children.Add(heading);

        Grid body = new() { ColumnSpacing = 20 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 2);

        Grid devicesPanel = new() { RowSpacing = 10 };
        devicesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        devicesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        devicesPanel.Children.Add(new TextBlock
        {
            Text = "Dispositivos conocidos",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        Grid.SetRow(DeviceList, 1);
        DeviceList.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        DeviceList.BorderThickness = new Thickness(0);
        DeviceList.SelectionChanged += DeviceList_SelectionChanged;
        devicesPanel.Children.Add(DeviceList);
        Border devicesCard = new()
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = devicesPanel
        };
        cards.Add(devicesCard);
        body.Children.Add(devicesCard);

        StackPanel editor = new() { Spacing = 12 };
        AliasTextBox.Header = "Nombre para este teclado";
        AliasTextBox.PlaceholderText = "Nombre reconocible";
        AliasTextBox.CornerRadius = new CornerRadius(8);
        editor.Children.Add(AliasTextBox);
        DetectedNameText.TextWrapping = TextWrapping.Wrap;
        TechnicalIdText.TextWrapping = TextWrapping.Wrap;
        StatusText.TextWrapping = TextWrapping.Wrap;
        secondaryText.Add(DetectedNameText);
        secondaryText.Add(TechnicalIdText);
        secondaryText.Add(StatusText);
        editor.Children.Add(DetectedNameText);
        editor.Children.Add(TechnicalIdText);
        editor.Children.Add(StatusText);
        LayoutComboBox.Header = "Distribución";
        LayoutComboBox.DisplayMemberPath = "Name";
        LayoutComboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        LayoutComboBox.CornerRadius = new CornerRadius(8);
        editor.Children.Add(LayoutComboBox);
        IgnoredCheckBox.Content = "Ignorar eventos de este dispositivo";
        IgnoredCheckBox.Checked += IgnoredCheckBox_Changed;
        IgnoredCheckBox.Unchecked += IgnoredCheckBox_Changed;
        editor.Children.Add(IgnoredCheckBox);
        StackPanel editorButtons = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        SaveButton.Content = "Guardar cambios";
        SaveButton.CornerRadius = new CornerRadius(8);
        SaveButton.Click += SaveButton_Click;
        ForgetButton.Content = "Olvidar dispositivo";
        ForgetButton.CornerRadius = new CornerRadius(8);
        ForgetButton.Click += ForgetButton_Click;
        buttons.Add(SaveButton);
        buttons.Add(ForgetButton);
        editorButtons.Children.Add(SaveButton);
        editorButtons.Children.Add(ForgetButton);
        editor.Children.Add(editorButtons);
        Border editorCard = new()
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = new ScrollViewer
            {
                Content = editor,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Auto
            }
        };
        cards.Add(editorCard);
        Grid.SetColumn(editorCard, 1);
        body.Children.Add(editorCard);
        root.Children.Add(body);

        Grid footer = new() { ColumnSpacing = 8 };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Button clear = new() { Content = "Limpiar preferencias" };
        clear.CornerRadius = new CornerRadius(8);
        buttons.Add(clear);
        clear.Click += ClearButton_Click;
        footer.Children.Add(clear);
        Button reload = new() { Content = "Recargar" };
        reload.CornerRadius = new CornerRadius(8);
        buttons.Add(reload);
        reload.Click += ReloadButton_Click;
        Grid.SetColumn(reload, 1);
        footer.Children.Add(reload);
        Button close = new() { Content = "Cerrar" };
        close.CornerRadius = new CornerRadius(8);
        buttons.Add(close);
        close.Click += CloseButton_Click;
        Grid.SetColumn(close, 2);
        footer.Children.Add(close);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        return root;
    }

    private void ConfigureCaptionButtons()
    {
        AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
    }

    private void ApplyFluentResources()
    {
        bool dark = contentRoot?.ActualTheme == ElementTheme.Dark;
        SolidColorBrush background = new(dark
            ? Color.FromArgb(0xC8, 0x2B, 0x28, 0x27)
            : Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF));
        SolidColorBrush stroke = new(dark
            ? Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x35, 0x00, 0x00, 0x00));
        SolidColorBrush foreground = new(dark
            ? Color.FromArgb(0xC8, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xA8, 0x00, 0x00, 0x00));

        AppWindow.TitleBar.ButtonForegroundColor = dark
            ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
        AppWindow.TitleBar.ButtonHoverBackgroundColor = dark
            ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x16, 0x00, 0x00, 0x00);

        foreach (Border card in cards)
        {
            card.Background = background;
            card.BorderBrush = stroke;
        }

        foreach (TextBlock text in secondaryText)
        {
            text.Foreground = foreground;
        }

        foreach (Button button in buttons)
        {
            button.CornerRadius = new CornerRadius(8);
            button.MinHeight = 36;
            button.Padding = new Thickness(14, 6, 14, 6);
        }

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out object accentStyle) &&
            accentStyle is Style style)
        {
            SaveButton.Style = style;
        }
    }

    private void TryEnableBackdrop()
    {
        try
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        catch
        {
            try
            {
                SystemBackdrop = new MicaBackdrop();
            }
            catch
            {
                SystemBackdrop = null;
            }
        }
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnActivated;
        double scale = (Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)Math.Ceiling(1020 * scale),
            (int)Math.Ceiling(760 * scale)));
        await ReloadAsync();
    }

    private async Task ReloadAsync(string? identityToSelect = null)
    {
        try
        {
            SetBusy(true);
            ApplySnapshot(await client.GetSnapshotAsync(), identityToSelect);
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo leer la configuración", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplySnapshot(SettingsSnapshot value, string? identityToSelect = null)
    {
        snapshot = value;
        rows.Clear();
        DeviceList.Items.Clear();
        foreach (SettingsDevice device in value.Devices)
        {
            SettingsLayout? layout = value.Layouts.FirstOrDefault(candidate =>
                candidate.Identifier == device.LayoutIdentifier);
            DeviceRow row = new(device, layout);
            rows.Add(row);
            DeviceList.Items.Add(CreateDeviceItem(row));
        }

        LayoutComboBox.Items.Clear();
        LayoutComboBox.Items.Add("Sin distribución");
        foreach (SettingsLayout layout in value.Layouts)
        {
            LayoutComboBox.Items.Add(layout);
        }

        DeviceRow? selected = rows.FirstOrDefault(row =>
            string.Equals(row.Identity, identityToSelect, StringComparison.OrdinalIgnoreCase)) ?? rows.FirstOrDefault();
        DeviceList.SelectedItem = DeviceList.Items.OfType<ListViewItem>()
            .FirstOrDefault(item => ReferenceEquals(item.Tag, selected));
        SetEditorEnabled(DeviceList.SelectedItem is not null);
    }

    private static ListViewItem CreateDeviceItem(DeviceRow row)
    {
        StackPanel content = new() { Spacing = 2 };
        content.Children.Add(new TextBlock
        {
            Text = row.DisplayName,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        content.Children.Add(new TextBlock
        {
            Text = row.Summary,
            Opacity = 0.72,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        return new ListViewItem
        {
            Tag = row,
            Content = content,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 2, 0, 2),
            Padding = new Thickness(10, 8, 10, 8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedRow is not DeviceRow row)
        {
            SetEditorEnabled(false);
            return;
        }

        AliasTextBox.Text = row.DisplayName;
        DetectedNameText.Text = $"Detectado: {row.DetectedName}";
        TechnicalIdText.Text = $"Identificador: {row.TechnicalId}";
        StatusText.Text = $"Estado: {(row.Connected ? "Conectado" : "Desconectado")} · Última detección: {row.LastSeenUtc.ToLocalTime():g}";
        IgnoredCheckBox.IsChecked = row.Ignored;
        LayoutComboBox.SelectedItem = row.Layout is null
            ? LayoutComboBox.Items[0]
            : LayoutComboBox.Items.OfType<SettingsLayout>()
                .FirstOrDefault(candidate => candidate.Identifier == row.Layout.Identifier);
        SetEditorEnabled(true);
    }

    private void IgnoredCheckBox_Changed(object sender, RoutedEventArgs e) =>
        LayoutComboBox.IsEnabled = SaveButton.IsEnabled && IgnoredCheckBox.IsChecked != true;

    private async void SaveButton_Click(object sender, RoutedEventArgs e) => await SaveSelectedAsync();

    private async Task SaveSelectedAsync()
    {
        if (SelectedRow is not DeviceRow row)
        {
            return;
        }

        try
        {
            SetBusy(true);
            SettingsLayout? layout = LayoutComboBox.SelectedItem as SettingsLayout;
            ApplySnapshot(await client.SaveAsync(
                row.Identity, AliasTextBox.Text, layout?.Identifier, IgnoredCheckBox.IsChecked == true), row.Identity);
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudieron guardar los cambios", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ForgetButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is not DeviceRow row)
        {
            return;
        }

        ContentDialog confirmation = Confirmation(
            "Olvidar dispositivo", $"Se olvidarán el nombre y la preferencia de \"{row.DisplayName}\".", "Olvidar");
        if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            SetBusy(true);
            ApplySnapshot(await client.ForgetAsync(row.Identity));
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo olvidar el dispositivo", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog confirmation = Confirmation(
            "Limpiar preferencias",
            "Se eliminarán todos los alias, distribuciones y dispositivos ignorados. Esta acción no se puede deshacer.",
            "Limpiar");
        if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            SetBusy(true);
            ApplySnapshot(await client.ClearAsync());
            StatusText.Text = "Las preferencias se limpiaron correctamente.";
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudieron limpiar las preferencias", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e) => await ReloadAsync(SelectedRow?.Identity);
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private ContentDialog Confirmation(string title, string content, string primaryText) => new()
    {
        XamlRoot = Content.XamlRoot,
        Title = title,
        Content = content,
        PrimaryButtonText = primaryText,
        CloseButtonText = "Cancelar",
        DefaultButton = ContentDialogButton.Close
    };

    private async Task ShowErrorAsync(string title, Exception error)
    {
        ContentDialog dialog = Confirmation(title, error.Message, "Aceptar");
        dialog.CloseButtonText = string.Empty;
        await dialog.ShowAsync();
    }

    private void SetBusy(bool busy)
    {
        DeviceList.IsEnabled = !busy;
        SetEditorEnabled(!busy && DeviceList.SelectedItem is not null);
    }

    private void SetEditorEnabled(bool enabled)
    {
        AliasTextBox.IsEnabled = enabled;
        LayoutComboBox.IsEnabled = enabled && IgnoredCheckBox.IsChecked != true;
        IgnoredCheckBox.IsEnabled = enabled;
        SaveButton.IsEnabled = enabled;
        ForgetButton.IsEnabled = enabled;
    }
}

public sealed class DeviceRow
{
    internal DeviceRow(SettingsDevice device, SettingsLayout? layout)
    {
        Identity = device.Identity;
        DisplayName = device.DisplayName;
        DetectedName = device.DetectedName;
        TechnicalId = device.TechnicalId;
        LastSeenUtc = device.LastSeenUtc;
        Connected = device.Connected;
        Ignored = device.Ignored;
        Layout = layout;
        string state = device.Ignored ? "Ignorado" : device.Connected ? "Conectado" : "Desconectado";
        Summary = layout is null ? state : $"{state} · {layout.Name}";
        AccessibleName = $"{DisplayName}. {Summary}";
    }

    public string Identity { get; }
    public string DisplayName { get; }
    public string DetectedName { get; }
    public string TechnicalId { get; }
    public DateTimeOffset LastSeenUtc { get; }
    public string Summary { get; }
    public string AccessibleName { get; }
    public bool Connected { get; }
    public bool Ignored { get; }
    internal SettingsLayout? Layout { get; }

    public override string ToString() => $"{DisplayName}\n{Summary}";
}
