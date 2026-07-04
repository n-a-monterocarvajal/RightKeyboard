using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media.Animation;
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
    private readonly CheckBox DiagnosticsCheckBox = new();
    private readonly List<Button> buttons = [];
    private readonly List<Border> cards = [];
    private readonly List<TextBlock> secondaryText = [];
    private readonly TextBlock activityText = new();
    private readonly Storyboard aliasEditingStoryboard = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer activityTimer;
    private Grid? contentRoot;
    private SettingsSnapshot? snapshot;
    private long lastActivitySequence;
    private bool pollingActivity;
    private bool applyingEditorState;
    private DateTimeOffset suppressActivitySelectionUntil;

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
        activityTimer = DispatcherQueue.CreateTimer();
        activityTimer.Interval = TimeSpan.FromMilliseconds(500);
        activityTimer.Tick += PollActivityAsync;
        Activated += OnActivated;
        Closed += (_, _) => activityTimer.Stop();
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
        Image appIcon = new()
        {
            Source = new BitmapImage(new Uri("ms-appx:///Assets/RightKeyboard.ico")),
            Width = 20,
            Height = 20,
            Stretch = Stretch.Uniform
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
        activityText.Text = "Pulsa una tecla para identificar visualmente su dispositivo.";
        activityText.FontSize = 12;
        activityText.Opacity = 0.78;
        secondaryText.Add(activityText);
        heading.Children.Add(activityText);
        ConfigureAliasEditingAnimation();
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
        AliasTextBox.TextChanged += AliasTextBox_TextChanged;
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
        StackPanel footerLeft = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        Button clear = new() { Content = "Limpiar preferencias" };
        clear.CornerRadius = new CornerRadius(8);
        buttons.Add(clear);
        clear.Click += ClearButton_Click;
        footerLeft.Children.Add(clear);
        DiagnosticsCheckBox.Content = "Diagnóstico detallado";
        DiagnosticsCheckBox.VerticalAlignment = VerticalAlignment.Center;
        DiagnosticsCheckBox.Click += DiagnosticsCheckBox_Click;
        footerLeft.Children.Add(DiagnosticsCheckBox);
        Button openDiagnostics = new() { Content = "Abrir registros" };
        openDiagnostics.CornerRadius = new CornerRadius(8);
        openDiagnostics.Click += OpenDiagnostics_Click;
        buttons.Add(openDiagnostics);
        footerLeft.Children.Add(openDiagnostics);
        footer.Children.Add(footerLeft);
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
        await ReloadDiagnosticsAsync();
        activityTimer.Start();
    }

    private async void PollActivityAsync(
        Microsoft.UI.Dispatching.DispatcherQueueTimer sender,
        object args)
    {
        if (pollingActivity)
        {
            return;
        }

        pollingActivity = true;
        try
        {
            SettingsActivity activity = await client.GetActivityAsync();
            if (activity.Sequence == 0 || activity.Sequence == lastActivitySequence || activity.Identity is null)
            {
                return;
            }

            lastActivitySequence = activity.Sequence;
            ListViewItem? item = DeviceList.Items.OfType<ListViewItem>()
                .FirstOrDefault(candidate =>
                    candidate.Tag is DeviceRow row &&
                    string.Equals(row.Identity, activity.Identity, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                await ReloadAsync(activity.Identity);
                item = DeviceList.Items.OfType<ListViewItem>()
                    .FirstOrDefault(candidate =>
                        candidate.Tag is DeviceRow row &&
                        string.Equals(row.Identity, activity.Identity, StringComparison.OrdinalIgnoreCase));
            }

            // Escribir un alias genera Raw Input desde el mismo teclado. El feedback
            // puede actualizar su texto, pero nunca debe mover selección ni foco.
            if (DateTimeOffset.UtcNow < suppressActivitySelectionUntil)
            {
                string name = item?.Tag is DeviceRow editingRow
                    ? editingRow.DisplayName
                    : "dispositivo pendiente de configurar";
                SetActivityText($"Entrada detectada: {name}");
                return;
            }

            if (item?.Tag is DeviceRow activeRow)
            {
                DeviceList.SelectedItem = item;
                DeviceList.ScrollIntoView(item);
                SetActivityText($"Entrada detectada: {activeRow.DisplayName}");
            }
            else
            {
                SetActivityText("Entrada detectada desde un dispositivo pendiente de configurar.");
            }
        }
        catch
        {
            // El feedback es auxiliar y nunca debe interrumpir la edición.
        }
        finally
        {
            pollingActivity = false;
        }
    }

    private void AliasTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!applyingEditorState && AliasTextBox.FocusState != FocusState.Unfocused)
        {
            suppressActivitySelectionUntil = DateTimeOffset.UtcNow.AddMilliseconds(900);
            ShowAliasEditingMessage();
        }
    }

    private void ConfigureAliasEditingAnimation()
    {
        DoubleAnimation fadeIn = new()
        {
            From = 0,
            To = 0.78,
            Duration = new Duration(TimeSpan.FromMilliseconds(180))
        };
        DoubleAnimation fadeOut = new()
        {
            From = 0.78,
            To = 0,
            BeginTime = TimeSpan.FromSeconds(4),
            Duration = new Duration(TimeSpan.FromMilliseconds(650))
        };
        Storyboard.SetTarget(fadeIn, activityText);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        Storyboard.SetTarget(fadeOut, activityText);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        aliasEditingStoryboard.Children.Add(fadeIn);
        aliasEditingStoryboard.Children.Add(fadeOut);
    }

    private void ShowAliasEditingMessage()
    {
        aliasEditingStoryboard.Stop();
        activityText.Text = "Editando nombre; la identificación se reanuda al dejar de escribir.";
        activityText.Opacity = 0;
        aliasEditingStoryboard.Begin();
    }

    private void SetActivityText(string text)
    {
        aliasEditingStoryboard.Stop();
        activityText.Opacity = 0.78;
        activityText.Text = text;
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
        IEnumerable<(SettingsDevice Device, SettingsLayout? Layout)> orderedDevices = value.Devices
            .Select(device =>
            {
                SettingsLayout? layout = value.Layouts.FirstOrDefault(candidate =>
                    candidate.Identifier == device.LayoutIdentifier);
                return (Device: device, Layout: layout);
            })
            .OrderBy(item => DeviceSortRank(item.Device, item.Layout))
            .ThenBy(item => item.Device.DisplayName, StringComparer.CurrentCultureIgnoreCase);

        foreach ((SettingsDevice device, SettingsLayout? layout) in orderedDevices)
        {
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

    private static int DeviceSortRank(SettingsDevice device, SettingsLayout? layout)
    {
        if (device.Ignored)
        {
            return 4;
        }

        if (device.Connected)
        {
            return layout is null ? 1 : 0;
        }

        return layout is null ? 3 : 2;
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

        applyingEditorState = true;
        AliasTextBox.Text = row.DisplayName;
        DetectedNameText.Text = $"Detectado: {row.DetectedName}";
        TechnicalIdText.Text = $"Identificador: {row.TechnicalId}";
        StatusText.Text = $"Estado: {(row.Connected ? "Conectado" : "Desconectado")} · Última detección: {row.LastSeenUtc.ToLocalTime():g}";
        IgnoredCheckBox.IsChecked = row.Ignored;
        LayoutComboBox.SelectedItem = row.Layout is null
            ? LayoutComboBox.Items[0]
            : LayoutComboBox.Items.OfType<SettingsLayout>()
                .FirstOrDefault(candidate => candidate.Identifier == row.Layout.Identifier);
        applyingEditorState = false;
        SetEditorEnabled(true);
    }

    private async Task ReloadDiagnosticsAsync()
    {
        try
        {
            DiagnosticsCheckBox.IsChecked = (await client.GetDiagnosticsAsync()).Enabled;
        }
        catch
        {
            DiagnosticsCheckBox.IsEnabled = false;
        }
    }

    private async void DiagnosticsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SettingsDiagnostics state = await client.SetDiagnosticsAsync(DiagnosticsCheckBox.IsChecked == true);
            DiagnosticsCheckBox.IsChecked = state.Enabled;
            SetActivityText(state.Enabled
                ? "Diagnóstico activo. Reproduce el problema y abre los registros."
                : "Diagnóstico desactivado.");
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo cambiar el diagnóstico", error);
        }
    }

    private async void OpenDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await client.OpenDiagnosticsAsync();
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo abrir la carpeta de registros", error);
        }
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

        if (!await ShowOverlayAsync(
                "Olvidar dispositivo",
                $"Se olvidarán el nombre y la preferencia de \"{row.DisplayName}\".",
                "Olvidar"))
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
        if (!await ShowOverlayAsync(
                "Limpiar preferencias",
                "Se eliminarán todos los alias, distribuciones y dispositivos ignorados. Esta acción no se puede deshacer.",
                "Limpiar"))
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

    private async Task ShowErrorAsync(string title, Exception error)
    {
        await ShowOverlayAsync(title, error.Message, "Aceptar", showCancel: false);
    }

    private Task<bool> ShowOverlayAsync(
        string title,
        string message,
        string primaryText,
        bool showCancel = true)
    {
        if (contentRoot is null)
        {
            return Task.FromResult(false);
        }

        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool dark = contentRoot.ActualTheme == ElementTheme.Dark;
        Grid overlay = new()
        {
            Background = new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRowSpan(overlay, 4);
        Canvas.SetZIndex(overlay, 100);

        StackPanel panelContent = new() { Spacing = 16 };
        panelContent.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        panelContent.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        Grid actions = new() { ColumnSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (showCancel)
        {
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        Button primary = new()
        {
            Content = primaryText,
            CornerRadius = new CornerRadius(8),
            MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        actions.Children.Add(primary);
        Button? cancel = null;
        if (showCancel)
        {
            cancel = new Button
            {
                Content = "Cancelar",
                CornerRadius = new CornerRadius(8),
                MinHeight = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out object style) && style is Style accent)
            {
                cancel.Style = accent;
            }
            Grid.SetColumn(cancel, 1);
            actions.Children.Add(cancel);
        }
        panelContent.Children.Add(actions);

        Border panel = new()
        {
            Width = 660,
            MaxWidth = 660,
            Padding = new Thickness(28),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(dark
                ? Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x38, 0, 0, 0)),
            Background = new SolidColorBrush(dark
                ? Color.FromArgb(0xFA, 0x20, 0x20, 0x20)
                : Color.FromArgb(0xFA, 0xFF, 0xFF, 0xFF)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = panelContent
        };
        overlay.Children.Add(panel);
        contentRoot.Children.Add(overlay);

        void Complete(bool result)
        {
            contentRoot.Children.Remove(overlay);
            completion.TrySetResult(result);
        }

        primary.Click += (_, _) => Complete(true);
        if (cancel is not null)
        {
            cancel.Click += (_, _) => Complete(false);
            cancel.Focus(FocusState.Programmatic);
        }
        else
        {
            primary.Focus(FocusState.Programmatic);
        }

        return completion.Task;
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
