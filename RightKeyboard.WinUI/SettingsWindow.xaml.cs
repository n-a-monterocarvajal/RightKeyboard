using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
    private readonly List<Border> cards = [];
    private readonly List<TextBlock> secondaryText = [];
    private SettingsSnapshot? snapshot;

    public SettingsWindow(SettingsIpcClient client)
    {
        this.client = client;
        Title = "Configuración de RightKeyboard";
        Content = BuildContent();
        ApplyFluentResources();
        TryEnableMica();
        DeviceList.ItemsSource = rows;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 680));
        Activated += OnActivated;
    }

    private DeviceRow? SelectedRow => DeviceList.SelectedItem as DeviceRow;

    private UIElement BuildContent()
    {
        Grid root = new() { Padding = new Thickness(24), RowSpacing = 16 };
        root.ActualThemeChanged += (_, _) => ApplyFluentResources();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
        root.Children.Add(heading);

        Grid body = new() { ColumnSpacing = 20 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);

        Grid devicesPanel = new() { RowSpacing = 10 };
        devicesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        devicesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        devicesPanel.Children.Add(new TextBlock
        {
            Text = "Dispositivos conocidos",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        Grid.SetRow(DeviceList, 1);
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
        editor.Children.Add(LayoutComboBox);
        IgnoredCheckBox.Content = "Ignorar eventos de este dispositivo";
        IgnoredCheckBox.Checked += IgnoredCheckBox_Changed;
        IgnoredCheckBox.Unchecked += IgnoredCheckBox_Changed;
        editor.Children.Add(IgnoredCheckBox);
        StackPanel editorButtons = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        SaveButton.Content = "Guardar cambios";
        SaveButton.Click += SaveButton_Click;
        ForgetButton.Content = "Olvidar dispositivo";
        ForgetButton.Click += ForgetButton_Click;
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
        clear.Click += ClearButton_Click;
        footer.Children.Add(clear);
        Button reload = new() { Content = "Recargar" };
        reload.Click += ReloadButton_Click;
        Grid.SetColumn(reload, 1);
        footer.Children.Add(reload);
        Button close = new() { Content = "Cerrar" };
        close.Click += CloseButton_Click;
        Grid.SetColumn(close, 2);
        footer.Children.Add(close);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        return root;
    }

    private void ApplyFluentResources()
    {
        if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out object cardBackground) &&
            cardBackground is Brush background)
        {
            foreach (Border card in cards)
            {
                card.Background = background;
            }
        }

        if (Application.Current.Resources.TryGetValue("CardStrokeColorDefaultBrush", out object cardStroke) &&
            cardStroke is Brush stroke)
        {
            foreach (Border card in cards)
            {
                card.BorderBrush = stroke;
            }
        }

        if (Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out object secondaryBrush) &&
            secondaryBrush is Brush foreground)
        {
            foreach (TextBlock text in secondaryText)
            {
                text.Foreground = foreground;
            }
        }

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out object accentStyle) &&
            accentStyle is Style style)
        {
            SaveButton.Style = style;
        }
    }

    private void TryEnableMica()
    {
        try
        {
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        }
        catch
        {
            SystemBackdrop = null;
        }
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnActivated;
        double scale = (Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)Math.Ceiling(980 * scale),
            (int)Math.Ceiling(680 * scale)));
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
        foreach (SettingsDevice device in value.Devices)
        {
            SettingsLayout? layout = value.Layouts.FirstOrDefault(candidate =>
                candidate.Identifier == device.LayoutIdentifier);
            rows.Add(new DeviceRow(device, layout));
        }

        LayoutComboBox.Items.Clear();
        LayoutComboBox.Items.Add("Sin distribución");
        foreach (SettingsLayout layout in value.Layouts)
        {
            LayoutComboBox.Items.Add(layout);
        }

        DeviceList.SelectedItem = rows.FirstOrDefault(row =>
            string.Equals(row.Identity, identityToSelect, StringComparison.OrdinalIgnoreCase)) ?? rows.FirstOrDefault();
        SetEditorEnabled(DeviceList.SelectedItem is not null);
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
