using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Composition;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace RightKeyboard.WinUI;

public sealed class SettingsWindow : Window
{
    private const int InitialWidth = 1080;
    private const int InitialHeight = 720;
    private const int MinimumWidth = 900;
    private const int MinimumHeight = 640;
    private const uint WmGetMinMaxInfo = 0x0024;
    private const nuint MinimumSizeSubclassId = 0x524B0007;

    private readonly SettingsIpcClient client;
    private readonly SettingsEditorStateTracker editorStateTracker = new();
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
    private readonly ComboBox GroupTargetComboBox = new();
    private readonly Button GroupButton = new();
    private readonly Button UngroupButton = new();
    private readonly StackPanel DeviceDetailsPanel = new() { Spacing = 2, Visibility = Visibility.Collapsed };
    private readonly StackPanel GroupButtonsPanel = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 8,
        Visibility = Visibility.Collapsed
    };
    private readonly CheckBox DiagnosticsCheckBox = new();
    private readonly CheckBox StartupCheckBox = new();
    private readonly List<Button> buttons = [];
    private readonly List<Border> cards = [];
    private readonly List<Border> separators = [];
    private readonly List<TextBlock> secondaryText = [];
    private readonly TextBlock activityText = new();
    private readonly TextBlock activityHintText = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer activityTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer activityHintTimer;
    private Grid? contentRoot;
    private SettingsSnapshot? snapshot;
    private long lastActivitySequence;
    private bool pollingActivity;
    private bool applyingEditorState;
    private bool suppressSelectionGuard;
    private bool closeConfirmed;
    private bool closeConfirmationPending;
    private ListViewItem? acceptedDeviceItem;
    private Visual? activityVisual;
    private Visual? activityHintVisual;
    private bool activityHintVisible;
    private NativeMethods.SubclassProc? minimumSizeSubclass;

    public SettingsWindow(SettingsIpcClient client)
    {
        this.client = client;
        Title = "Configuración de RightKeyboard";
        ExtendsContentIntoTitleBar = true;
        Content = BuildContent();
        ConfigureCaptionButtons();
        ApplyFluentResources();
        TryEnableBackdrop();
        ConfigureMinimumSize();
        ResizeForCurrentDpi(InitialWidth, InitialHeight);
        activityTimer = DispatcherQueue.CreateTimer();
        activityTimer.Interval = TimeSpan.FromMilliseconds(500);
        activityTimer.Tick += PollActivityAsync;
        activityHintTimer = DispatcherQueue.CreateTimer();
        activityHintTimer.Interval = TimeSpan.FromMilliseconds(1400);
        activityHintTimer.Tick += HideAliasEditingMessage;
        Activated += OnActivated;
        AppWindow.Closing += AppWindow_Closing;
        Closed += (_, _) =>
        {
            activityTimer.Stop();
            activityHintTimer.Stop();
            RemoveMinimumSizeSubclass();
        };
    }

    private DeviceRow? SelectedRow => (DeviceList.SelectedItem as ListViewItem)?.Tag as DeviceRow;

    private UIElement BuildContent()
    {
        Grid root = new() { Padding = new Thickness(24, 0, 24, 24), RowSpacing = 16 };
        contentRoot = root;
        root.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        root.ActualThemeChanged += (_, _) => ApplyFluentResources();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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
        StackPanel appIdentity = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 0
        };
        appIdentity.Children.Add(new TextBlock
        {
            Text = "RightKeyboard",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        appIdentity.Children.Add(new TextBlock
        {
            Text = VersionPresentation.Current,
            FontSize = 11,
            Opacity = 0.68
        });
        Grid.SetColumn(appIdentity, 1);
        titleBar.Children.Add(appIdentity);
        root.Children.Add(titleBar);
        SetTitleBar(titleBar);

        // Cuerpo en dos columnas: el inventario cede altura y desplaza su lista cuando es
        // necesario; el editor conserva el espacio operativo y sus acciones siempre visibles.
        Grid body = new() { ColumnSpacing = 20 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);

        // ---- Columna izquierda: título, lista jerárquica y comandos de archivo ----
        Grid leftColumn = new() { RowSpacing = 12 };
        leftColumn.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftColumn.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        leftColumn.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        StackPanel heading = new() { Spacing = 4 };
        heading.Children.Add(new TextBlock
        {
            Text = "Teclados y preferencias",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        TextBlock subtitle = new()
        {
            Text = "Administra los teclados detectados.",
            TextWrapping = TextWrapping.Wrap
        };
        secondaryText.Add(subtitle);
        heading.Children.Add(subtitle);
        activityText.Text = "Pulsa una tecla para identificar su dispositivo.";
        activityText.FontSize = 12;
        activityText.Opacity = 1;
        activityText.TextWrapping = TextWrapping.Wrap;
        secondaryText.Add(activityText);
        activityHintText.Text = "· La identificación se reanudará al dejar de escribir.";
        activityHintText.FontSize = 12;
        activityHintText.Opacity = 1;
        activityHintText.TextWrapping = TextWrapping.Wrap;
        // Oculto salvo durante la edición del alias: colapsado para no reservar altura.
        activityHintText.Visibility = Visibility.Collapsed;
        secondaryText.Add(activityHintText);
        heading.Children.Add(activityText);
        heading.Children.Add(activityHintText);
        Grid.SetRow(heading, 0);
        leftColumn.Children.Add(heading);

        Grid devicesPanel = new() { RowSpacing = 10 };
        devicesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        devicesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid devicesHeader = new() { ColumnSpacing = 8 };
        devicesHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        devicesHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        devicesHeader.Children.Add(new TextBlock
        {
            Text = "Dispositivos conocidos",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        Button reload = new() { Content = "Recargar", VerticalAlignment = VerticalAlignment.Center };
        reload.CornerRadius = new CornerRadius(8);
        buttons.Add(reload);
        reload.Click += ReloadButton_Click;
        Grid.SetColumn(reload, 1);
        devicesHeader.Children.Add(reload);
        devicesPanel.Children.Add(devicesHeader);
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
        Grid.SetRow(devicesCard, 1);
        leftColumn.Children.Add(devicesCard);

        StackPanel preferenceActions = new() { Spacing = 8 };
        preferenceActions.Children.Add(new TextBlock
        {
            Text = "Preferencias",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        Grid fileButtons = new() { ColumnSpacing = 8 };
        fileButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fileButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Button export = new()
        {
            Content = "Exportar",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        export.CornerRadius = new CornerRadius(8);
        export.Click += ExportButton_Click;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(export, "ExportPreferencesButton");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(export, "Exportar preferencias a un archivo");
        buttons.Add(export);
        fileButtons.Children.Add(export);
        Button import = new()
        {
            Content = "Importar",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        import.CornerRadius = new CornerRadius(8);
        import.Click += ImportButton_Click;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(import, "ImportPreferencesButton");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(import, "Importar preferencias desde un archivo");
        buttons.Add(import);
        Grid.SetColumn(import, 1);
        fileButtons.Children.Add(import);
        preferenceActions.Children.Add(fileButtons);
        preferenceActions.Children.Add(CreateSeparator());

        Grid resetRow = new() { ColumnSpacing = 8 };
        resetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        resetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        StackPanel resetDescription = new() { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        resetDescription.Children.Add(new TextBlock
        {
            Text = "Restablecer",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        TextBlock resetSupportingText = new()
        {
            Text = "Elimina todas las preferencias.",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        secondaryText.Add(resetSupportingText);
        resetDescription.Children.Add(resetSupportingText);
        resetRow.Children.Add(resetDescription);
        Button clear = new()
        {
            Content = "Limpiar preferencias",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        clear.CornerRadius = new CornerRadius(8);
        clear.Click += ClearButton_Click;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(clear, "ClearPreferencesButton");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(
            clear,
            "Acción destructiva que solicita confirmación antes de eliminar todas las preferencias.");
        ApplyDestructiveButtonResources(clear);
        buttons.Add(clear);
        Grid.SetColumn(clear, 1);
        resetRow.Children.Add(clear);
        preferenceActions.Children.Add(resetRow);

        StackPanel systemPreferences = new() { Spacing = 8 };
        Grid systemRow = new() { ColumnSpacing = 8 };
        systemRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        systemRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        systemRow.Children.Add(new TextBlock
        {
            Text = "Sistema",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        StartupCheckBox.Content = "Iniciar con Windows";
        StartupCheckBox.Click += StartupCheckBox_Click;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
            StartupCheckBox,
            "StartWithWindowsCheckBox");
        Grid.SetColumn(StartupCheckBox, 1);
        systemRow.Children.Add(StartupCheckBox);
        systemPreferences.Children.Add(systemRow);
        if (DiagnosticLogger.IsAvailable)
        {
            systemPreferences.Children.Add(CreateSeparator());
            systemPreferences.Children.Add(new TextBlock
            {
                Text = "Diagnóstico",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            Grid diagnosticsRow = new() { ColumnSpacing = 8 };
            diagnosticsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            diagnosticsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            DiagnosticsCheckBox.Content = "Diagnóstico detallado";
            DiagnosticsCheckBox.VerticalAlignment = VerticalAlignment.Center;
            DiagnosticsCheckBox.Click += DiagnosticsCheckBox_Click;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                DiagnosticsCheckBox,
                "DetailedDiagnosticsCheckBox");
            diagnosticsRow.Children.Add(DiagnosticsCheckBox);
            Button openDiagnostics = new() { Content = "Abrir registros" };
            openDiagnostics.CornerRadius = new CornerRadius(8);
            openDiagnostics.Click += OpenDiagnostics_Click;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                openDiagnostics,
                "OpenDiagnosticsButton");
            buttons.Add(openDiagnostics);
            Grid.SetColumn(openDiagnostics, 1);
            diagnosticsRow.Children.Add(openDiagnostics);
            systemPreferences.Children.Add(diagnosticsRow);
        }

        StackPanel leftFooter = new() { Spacing = 12 };
        leftFooter.Children.Add(CreateCard(preferenceActions, new Thickness(12)));
        leftFooter.Children.Add(CreateCard(systemPreferences, new Thickness(12)));
        Grid.SetRow(leftFooter, 2);
        leftColumn.Children.Add(leftFooter);

        Grid.SetColumn(leftColumn, 0);
        body.Children.Add(leftColumn);

        // ---- Columna derecha: editor estable del dispositivo seleccionado ----
        StackPanel editorFields = new() { Spacing = 12 };
        editorFields.Children.Add(CreateSectionHeading(
            "Dispositivo seleccionado",
            "Edita el grupo lógico o el teclado seleccionado. Las identidades técnicas agrupadas permanecen visibles en la lista."));
        AliasTextBox.Header = "Nombre para este teclado";
        AliasTextBox.PlaceholderText = "Nombre reconocible";
        AliasTextBox.CornerRadius = new CornerRadius(8);
        ApplyRoundedTextBoxResources(AliasTextBox);
        AliasTextBox.TextChanged += AliasTextBox_TextChanged;
        AliasTextBox.KeyDown += AliasTextBox_KeyDown;
        editorFields.Children.Add(AliasTextBox);
        DetectedNameText.TextWrapping = TextWrapping.Wrap;
        TechnicalIdText.TextWrapping = TextWrapping.Wrap;
        StatusText.TextWrapping = TextWrapping.Wrap;
        secondaryText.Add(DetectedNameText);
        secondaryText.Add(TechnicalIdText);
        secondaryText.Add(StatusText);
        DeviceDetailsPanel.Children.Add(DetectedNameText);
        DeviceDetailsPanel.Children.Add(TechnicalIdText);
        DeviceDetailsPanel.Children.Add(StatusText);
        editorFields.Children.Add(DeviceDetailsPanel);
        LayoutComboBox.Header = "Distribución";
        LayoutComboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        LayoutComboBox.CornerRadius = new CornerRadius(8);
        LayoutComboBox.SelectionChanged += EditorField_Changed;
        editorFields.Children.Add(LayoutComboBox);
        IgnoredCheckBox.Content = "Ignorar eventos de este dispositivo";
        IgnoredCheckBox.Checked += IgnoredCheckBox_Changed;
        IgnoredCheckBox.Unchecked += IgnoredCheckBox_Changed;
        editorFields.Children.Add(IgnoredCheckBox);
        GroupTargetComboBox.Header = "Agrupar con otra identidad";
        GroupTargetComboBox.PlaceholderText = "Selecciona una identidad técnica";
        GroupTargetComboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        GroupTargetComboBox.CornerRadius = new CornerRadius(8);
        GroupTargetComboBox.SelectionChanged += (_, _) =>
            GroupButton.IsEnabled = GroupTargetComboBox.IsEnabled && GroupTargetComboBox.SelectedItem is DeviceRow;
        editorFields.Children.Add(GroupTargetComboBox);
        GroupButton.Content = "Agrupar identidades";
        GroupButton.CornerRadius = new CornerRadius(8);
        GroupButton.Click += GroupButton_Click;
        UngroupButton.Content = "Separar del grupo";
        UngroupButton.CornerRadius = new CornerRadius(8);
        UngroupButton.Click += UngroupButton_Click;
        buttons.Add(GroupButton);
        buttons.Add(UngroupButton);
        GroupButtonsPanel.Children.Add(GroupButton);
        GroupButtonsPanel.Children.Add(UngroupButton);
        editorFields.Children.Add(GroupButtonsPanel);
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
        Grid editorLayout = new() { RowSpacing = 12, MinHeight = 440 };
        editorLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        editorLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        ScrollViewer editorScroll = new()
        {
            Content = editorFields,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Auto
        };
        editorLayout.Children.Add(editorScroll);
        Grid.SetRow(editorButtons, 1);
        editorLayout.Children.Add(editorButtons);
        Border editorCard = CreateCard(editorLayout, new Thickness(20));
        editorCard.MinHeight = 440;
        Grid.SetColumn(editorCard, 1);
        body.Children.Add(editorCard);
        root.Children.Add(body);
        return root;
    }

    private StackPanel CreateSectionHeading(string title, string description)
    {
        StackPanel heading = new() { Spacing = 2 };
        heading.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        TextBlock supportingText = new()
        {
            Text = description,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        secondaryText.Add(supportingText);
        heading.Children.Add(supportingText);
        return heading;
    }

    private Border CreateCard(UIElement child, Thickness padding)
    {
        Border card = new()
        {
            Padding = padding,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = child
        };
        cards.Add(card);
        return card;
    }

    private Border CreateSeparator()
    {
        Border separator = new()
        {
            Height = 1,
            Margin = new Thickness(0, 2, 0, 2)
        };
        separators.Add(separator);
        return separator;
    }

    private static void ApplyDestructiveButtonResources(Button button)
    {
        ResourceDictionary light = new();
        light["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFD, 0xE7, 0xE9));
        light["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C));
        light["ButtonForegroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xA4, 0x26, 0x2C));
        light["ButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xF1, 0xAE, 0xB5));
        light["ButtonBorderBrushPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xA4, 0x26, 0x2C));
        light["ButtonForegroundPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x5C, 0x0B, 0x0E));
        ResourceDictionary dark = new();
        dark["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x5A, 0x1D, 0x1D));
        dark["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x99, 0xA4));
        dark["ButtonForegroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        dark["ButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0x7A, 0x1F, 0x28));
        dark["ButtonBorderBrushPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xB3, 0xBA));
        dark["ButtonForegroundPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        button.Resources.ThemeDictionaries["Light"] = light;
        button.Resources.ThemeDictionaries["Dark"] = dark;
    }

    private void ConfigureMinimumSize()
    {
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        minimumSizeSubclass = EnforceMinimumSize;
        if (!NativeMethods.SetWindowSubclass(hwnd, minimumSizeSubclass, MinimumSizeSubclassId, 0))
        {
            minimumSizeSubclass = null;
        }
    }

    private void RemoveMinimumSizeSubclass()
    {
        if (minimumSizeSubclass is null)
        {
            return;
        }

        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeMethods.RemoveWindowSubclass(hwnd, minimumSizeSubclass, MinimumSizeSubclassId);
        minimumSizeSubclass = null;
    }

    private nint EnforceMinimumSize(
        nint hwnd,
        uint message,
        nint wParam,
        nint lParam,
        nuint subclassId,
        nuint referenceData)
    {
        if (message == WmGetMinMaxInfo)
        {
            NativeMethods.MinMaxInfo limits = Marshal.PtrToStructure<NativeMethods.MinMaxInfo>(lParam);
            uint dpi = NativeMethods.GetDpiForWindow(hwnd);
            limits.MinTrackSize.X = ScaleForDpi(MinimumWidth, dpi);
            limits.MinTrackSize.Y = ScaleForDpi(MinimumHeight, dpi);
            Marshal.StructureToPtr(limits, lParam, false);
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void ResizeForCurrentDpi(int logicalWidth, int logicalHeight)
    {
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = NativeMethods.GetDpiForWindow(hwnd);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            ScaleForDpi(logicalWidth, dpi),
            ScaleForDpi(logicalHeight, dpi)));
    }

    private static int ScaleForDpi(int logicalPixels, uint dpi) =>
        (int)Math.Ceiling(logicalPixels * Math.Max(dpi, 96u) / 96d);

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

        foreach (Border separator in separators)
        {
            separator.Background = stroke;
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
        GetActivityVisual().Opacity = 0.78f;
        GetActivityHintVisual().Opacity = 0;
        ResizeForCurrentDpi(InitialWidth, InitialHeight);
        await ReloadAsync();
        await ReloadStartupAsync();
        if (DiagnosticLogger.IsAvailable)
        {
            await ReloadDiagnosticsAsync();
        }
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
            if (AliasTextBox.FocusState != FocusState.Unfocused)
            {
                string name = item?.Tag is DeviceRow editingRow
                    ? editingRow.DisplayName
                    : "dispositivo pendiente de configurar";
                string identifier = item?.Tag is DeviceRow row ? row.TechnicalId : string.Empty;
                SetActivityText(FormatDetectedActivity(name, identifier));
                return;
            }

            if (item?.Tag is DeviceRow activeRow)
            {
                DeviceList.SelectedItem = item;
                DeviceList.ScrollIntoView(item);
                SetActivityText(FormatDetectedActivity(activeRow.DisplayName, activeRow.TechnicalId));
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
        TrackEditorState();
        if (!applyingEditorState && AliasTextBox.FocusState != FocusState.Unfocused)
        {
            ShowAliasEditingMessage();
        }
    }

    private async void AliasTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await SaveSelectedAsync();
            DeviceList.Focus(FocusState.Programmatic);
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            if (SelectedRow is DeviceRow row)
            {
                applyingEditorState = true;
                AliasTextBox.Text = row.DisplayName;
                applyingEditorState = false;
                TrackEditorState();
            }
            DeviceList.Focus(FocusState.Programmatic);
        }
    }

    private void ShowAliasEditingMessage()
    {
        activityHintTimer.Stop();
        activityHintTimer.Start();
        if (activityHintVisible)
        {
            return;
        }

        activityHintVisible = true;
        activityHintText.Visibility = Visibility.Visible;
        AnimateOpacity(GetActivityVisual(), 0.78f, 0.52f, 180);
        AnimateOpacity(GetActivityHintVisual(), 0, 0.78f, 180);
    }

    private void HideAliasEditingMessage(
        Microsoft.UI.Dispatching.DispatcherQueueTimer sender,
        object args)
    {
        sender.Stop();
        if (!activityHintVisible)
        {
            return;
        }

        activityHintVisible = false;
        AnimateOpacity(GetActivityVisual(), 0.52f, 0.78f, 500);
        AnimateOpacity(GetActivityHintVisual(), 0.78f, 0, 500);
        activityHintText.Visibility = Visibility.Collapsed;
    }

    private void SetActivityText(string text)
    {
        activityText.Text = text;
        GetActivityVisual().Opacity = activityHintVisible ? 0.52f : 0.78f;
    }

    private Visual GetActivityVisual()
    {
        return activityVisual ??= ElementCompositionPreview.GetElementVisual(activityText);
    }

    private Visual GetActivityHintVisual() =>
        activityHintVisual ??= ElementCompositionPreview.GetElementVisual(activityHintText);

    private static void AnimateOpacity(Visual visual, float from, float to, int milliseconds)
    {
        try
        {
            visual.StopAnimation("Opacity");
            visual.Opacity = to;
            ScalarKeyFrameAnimation animation = visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0, from);
            animation.InsertKeyFrame(1, to);
            animation.Duration = TimeSpan.FromMilliseconds(milliseconds);
            visual.StartAnimation("Opacity", animation);
        }
        catch
        {
            visual.Opacity = to;
        }
    }

    private static string FormatDetectedActivity(string name, string? technicalId) =>
        string.IsNullOrWhiteSpace(technicalId)
            ? $"Entrada detectada: {name}"
            : $"Entrada detectada: {name} · {technicalId}";

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
        suppressSelectionGuard = true;
        try
        {
            acceptedDeviceItem = null;
            editorStateTracker.Clear();
            rows.Clear();
            DeviceList.Items.Clear();
            Dictionary<string, SettingsDevice> devicesByIdentity = value.Devices
                .ToDictionary(device => device.Identity, StringComparer.OrdinalIgnoreCase);
            foreach (SettingsDeviceGroup group in value.Groups.OrderBy(
                         group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                SettingsLayout? groupLayout = value.Layouts.FirstOrDefault(candidate =>
                    candidate.Identifier == group.LayoutIdentifier);
                DeviceRow groupRow = new(group, groupLayout, devicesByIdentity);
                AddRow(groupRow);
                foreach (string memberIdentity in group.MemberIdentities)
                {
                    if (devicesByIdentity.TryGetValue(memberIdentity, out SettingsDevice? member))
                    {
                        AddRow(new DeviceRow(member, group.Id));
                    }
                }
            }

            IEnumerable<(SettingsDevice Device, SettingsLayout? Layout)> orderedDevices = value.Devices
                .Where(device => device.GroupId is null)
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
                AddRow(new DeviceRow(device, layout));
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
        }
        finally
        {
            suppressSelectionGuard = false;
        }

        ApplySelectedDevice(DeviceList.SelectedItem as ListViewItem);
        SetEditorEnabled(DeviceList.SelectedItem is not null);
    }

    private void AddRow(DeviceRow row)
    {
        rows.Add(row);
        DeviceList.Items.Add(CreateDeviceItem(row));
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
        ListViewItem item = new()
        {
            Tag = row,
            Content = content,
            CornerRadius = new CornerRadius(8),
            Margin = row.IsGroupMember ? new Thickness(20, 0, 0, 0) : new Thickness(0, 2, 0, 2),
            Padding = new Thickness(10, 8, 10, 8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, row.AccessibleName);
        return item;
    }

    private async void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressSelectionGuard)
        {
            return;
        }

        ListViewItem? requestedItem = DeviceList.SelectedItem as ListViewItem;
        if (ReferenceEquals(requestedItem, acceptedDeviceItem))
        {
            return;
        }

        if (acceptedDeviceItem is not null && editorStateTracker.IsDirty)
        {
            SetSelectedItemWithoutHandling(acceptedDeviceItem);
            if (!await ConfirmDiscardChangesAsync())
            {
                acceptedDeviceItem.Focus(FocusState.Programmatic);
                return;
            }

            SetSelectedItemWithoutHandling(requestedItem);
        }

        ApplySelectedDevice(requestedItem);
    }

    private void ApplySelectedDevice(ListViewItem? item)
    {
        acceptedDeviceItem = item;
        if (item?.Tag is not DeviceRow row)
        {
            editorStateTracker.Clear();
            SetEditorEnabled(false);
            return;
        }

        applyingEditorState = true;
        try
        {
            AliasTextBox.Text = row.DisplayName;
            DetectedNameText.Text = row.IsGroup
                ? $"Grupo lógico con {row.MemberCount} identidades técnicas"
                : $"Detectado: {row.DetectedName}";
            TechnicalIdText.Text = row.IsGroup
                ? "Selecciona una identidad secundaria para ver o separarla."
                : $"Identificador: {row.TechnicalId}";
            StatusText.Text = row.IsGroup
                ? $"Estado: {(row.Connected ? "Algún miembro conectado" : "Desconectado")}"
                : $"Estado: {(row.Connected ? "Conectado" : "Desconectado")} · Última detección: {row.LastSeenUtc.ToLocalTime():g}";
            IgnoredCheckBox.IsChecked = row.Ignored;
            LayoutComboBox.SelectedItem = row.Layout is null
                ? LayoutComboBox.Items[0]
                : LayoutComboBox.Items.OfType<SettingsLayout>()
                    .FirstOrDefault(candidate => candidate.Identifier == row.Layout.Identifier);
            GroupTargetComboBox.Items.Clear();
            foreach (DeviceRow candidate in rows.Where(candidate =>
                         candidate.CanBeGroupTarget &&
                         !string.Equals(candidate.GroupId, row.GroupId, StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(candidate.TargetIdentity, row.TargetIdentity, StringComparison.OrdinalIgnoreCase)))
            {
                GroupTargetComboBox.Items.Add(candidate);
            }
            GroupTargetComboBox.SelectedIndex = -1;
        }
        finally
        {
            applyingEditorState = false;
        }

        editorStateTracker.Load(CreateEditorState());
        SetEditorEnabled(true);
    }

    private void SetSelectedItemWithoutHandling(ListViewItem? item)
    {
        suppressSelectionGuard = true;
        try
        {
            DeviceList.SelectedItem = item;
        }
        finally
        {
            suppressSelectionGuard = false;
        }
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

    private async Task ReloadStartupAsync()
    {
        try
        {
            StartupCheckBox.IsChecked = (await client.GetStartupAsync()).Enabled;
        }
        catch
        {
            StartupCheckBox.IsEnabled = false;
        }
    }

    private async void StartupCheckBox_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SettingsStartup state = await client.SetStartupAsync(StartupCheckBox.IsChecked == true);
            StartupCheckBox.IsChecked = state.Enabled;
            SetActivityText(state.Enabled
                ? "RightKeyboard se iniciará con Windows."
                : "RightKeyboard no se iniciará con Windows.");
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo cambiar el inicio con Windows", error);
            await ReloadStartupAsync();
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

    private void IgnoredCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        LayoutComboBox.IsEnabled = SaveButton.IsEnabled && IgnoredCheckBox.IsChecked != true;
        TrackEditorState();
    }

    private void EditorField_Changed(object sender, SelectionChangedEventArgs e) => TrackEditorState();

    private void TrackEditorState() => editorStateTracker.Update(CreateEditorState(), applyingEditorState);

    private SettingsEditorState CreateEditorState() => new(
        AliasTextBox.Text,
        (LayoutComboBox.SelectedItem as SettingsLayout)?.Identifier,
        IgnoredCheckBox.IsChecked == true);

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!editorStateTracker.IsDirty)
        {
            return true;
        }

        bool discard = await ShowOverlayAsync(
            "Cambios sin guardar",
            "Cambiaste el alias, la distribución o el estado ignorado. Si continúas, esos cambios se descartarán.",
            "Descartar cambios");
        return editorStateTracker.CanLeave(discard
            ? UnsavedChangesDecision.Discard
            : UnsavedChangesDecision.KeepEditing);
    }

    private async void AppWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (closeConfirmed || !editorStateTracker.IsDirty)
        {
            return;
        }

        args.Cancel = true;
        if (closeConfirmationPending)
        {
            return;
        }

        closeConfirmationPending = true;
        try
        {
            if (await ConfirmDiscardChangesAsync())
            {
                closeConfirmed = true;
                Close();
            }
        }
        finally
        {
            closeConfirmationPending = false;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e) => await SaveSelectedAsync();

    private async Task SaveSelectedAsync()
    {
        if (SelectedRow is not DeviceRow row || row.IsGroupMember)
        {
            return;
        }

        try
        {
            SetBusy(true);
            SettingsLayout? layout = LayoutComboBox.SelectedItem as SettingsLayout;
            ApplySnapshot(await client.SaveAsync(
                row.TargetIdentity, AliasTextBox.Text, layout?.Identifier, IgnoredCheckBox.IsChecked == true), row.Identity);
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
        if (SelectedRow is not DeviceRow row || row.IsGroup || row.IsGroupMember)
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
            ApplySnapshot(await client.ForgetAsync(row.TargetIdentity));
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

    private async void GroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is not DeviceRow row || GroupTargetComboBox.SelectedItem is not DeviceRow target)
        {
            return;
        }

        if (!await ShowOverlayAsync(
                "Agrupar identidades",
                $"\"{AliasTextBox.Text.Trim()}\" gobernará el alias y la distribución compartidos con \"{target.DisplayName}\". Las identidades técnicas seguirán visibles y podrán separarse.",
                "Agrupar"))
        {
            return;
        }

        try
        {
            SetBusy(true);
            SettingsLayout? layout = LayoutComboBox.SelectedItem as SettingsLayout;
            SettingsSnapshot grouped = await client.GroupAsync(
                row.TargetIdentity,
                target.TargetIdentity,
                AliasTextBox.Text,
                layout?.Identifier);
            string? groupId = grouped.Groups.FirstOrDefault(group =>
                group.MemberIdentities.Contains(row.TargetIdentity, StringComparer.OrdinalIgnoreCase))?.Id;
            ApplySnapshot(grouped, groupId ?? row.TargetIdentity);
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudieron agrupar las identidades", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void UngroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is not DeviceRow { IsGroupMember: true } row)
        {
            return;
        }

        if (!await ShowOverlayAsync(
                "Separar identidad",
                $"\"{row.DisplayName}\" dejará el grupo y recuperará sus preferencias individuales anteriores.",
                "Separar"))
        {
            return;
        }

        try
        {
            SetBusy(true);
            ApplySnapshot(await client.UngroupAsync(row.TargetIdentity), row.TargetIdentity);
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo separar la identidad", error);
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
                "Se eliminarán todos los alias, distribuciones, grupos y dispositivos ignorados. Esta acción no se puede deshacer.",
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

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        FileSavePicker picker = new()
        {
            SuggestedFileName = $"RightKeyboard-preferencias-{DateTime.Now:yyyy-MM-dd}",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeChoices.Add("Configuración JSON", [".json"]);
        InitializeWithWindow(picker);
        StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await client.ExportAsync(file.Path);
            SetActivityText("Las preferencias se exportaron correctamente.");
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudieron exportar las preferencias", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new() { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow(picker);
        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        SettingsImportPreview preview;
        try
        {
            preview = await client.ImportPreviewAsync(file.Path);
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo leer el archivo", error);
            return;
        }

        bool? replace = await ShowImportChoiceAsync(preview);
        if (replace is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            ApplySnapshot(await client.ImportApplyAsync(file.Path, replace.Value));
            SetActivityText("Las preferencias se importaron correctamente.");
        }
        catch (Exception error)
        {
            await ShowErrorAsync("No se pudo aplicar la importación", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void InitializeWithWindow(object target)
    {
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(target, hwnd);
    }

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
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(-24, 0, -24, -24),
            TabFocusNavigation = KeyboardNavigationMode.Cycle
        };
        Grid.SetRowSpan(overlay, 2);
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
        _ = AnimateOverlayAsync(overlay, panel, showing: true);

        bool closing = false;
        async void Complete(bool result)
        {
            if (closing) return;
            closing = true;
            primary.IsEnabled = false;
            if (cancel is not null) cancel.IsEnabled = false;
            await AnimateOverlayAsync(overlay, panel, showing: false);
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

        KeyboardAccelerator escape = new() { Key = Windows.System.VirtualKey.Escape };
        escape.Invoked += (_, args) =>
        {
            args.Handled = true;
            Complete(false);
        };
        overlay.KeyboardAccelerators.Add(escape);

        return completion.Task;
    }

    // Importar ofrece tres salidas (reemplazar / combinar / cancelar), por lo que
    // no encaja en ShowOverlayAsync (primaria + cancelar). Devuelve true=reemplazar,
    // false=combinar, null=cancelar.
    private Task<bool?> ShowImportChoiceAsync(SettingsImportPreview preview)
    {
        if (contentRoot is null)
        {
            return Task.FromResult<bool?>(null);
        }

        string message = $"Se encontraron {preview.DeviceCount} dispositivos en el archivo.";
        if (preview.Warnings.Count > 0)
        {
            message += "\n\nAdvertencias:\n- " + string.Join("\n- ", preview.Warnings);
        }

        message += "\n\nReemplazar sustituye la configuración actual. Combinar la fusiona con " +
            "la existente. En ambos casos se crea un respaldo automático.";

        TaskCompletionSource<bool?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool dark = contentRoot.ActualTheme == ElementTheme.Dark;
        Grid overlay = new()
        {
            Background = new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(-24, 0, -24, -24),
            TabFocusNavigation = KeyboardNavigationMode.Cycle
        };
        Grid.SetRowSpan(overlay, 2);
        Canvas.SetZIndex(overlay, 100);

        StackPanel panelContent = new() { Spacing = 16 };
        panelContent.Children.Add(new TextBlock
        {
            Text = "Importar preferencias",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        panelContent.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        Grid actions = new() { ColumnSpacing = 8 };
        for (int column = 0; column < 3; column++)
        {
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        Button replace = new()
        {
            Content = "Reemplazar",
            CornerRadius = new CornerRadius(8),
            MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Button merge = new()
        {
            Content = "Combinar",
            CornerRadius = new CornerRadius(8),
            MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out object style) && style is Style accent)
        {
            merge.Style = accent;
        }

        Button cancel = new()
        {
            Content = "Cancelar",
            CornerRadius = new CornerRadius(8),
            MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(merge, 1);
        Grid.SetColumn(cancel, 2);
        actions.Children.Add(replace);
        actions.Children.Add(merge);
        actions.Children.Add(cancel);
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
        _ = AnimateOverlayAsync(overlay, panel, showing: true);

        bool closing = false;
        async void Complete(bool? result)
        {
            if (closing) return;
            closing = true;
            replace.IsEnabled = false;
            merge.IsEnabled = false;
            cancel.IsEnabled = false;
            await AnimateOverlayAsync(overlay, panel, showing: false);
            contentRoot.Children.Remove(overlay);
            completion.TrySetResult(result);
        }

        replace.Click += (_, _) => Complete(true);
        merge.Click += (_, _) => Complete(false);
        cancel.Click += (_, _) => Complete(null);
        merge.Focus(FocusState.Programmatic);

        KeyboardAccelerator escape = new() { Key = Windows.System.VirtualKey.Escape };
        escape.Invoked += (_, args) =>
        {
            args.Handled = true;
            Complete(null);
        };
        overlay.KeyboardAccelerators.Add(escape);

        return completion.Task;
    }

    private static async Task AnimateOverlayAsync(Grid overlay, Border panel, bool showing)
    {
        Visual overlayVisual = ElementCompositionPreview.GetElementVisual(overlay);
        Visual panelVisual = ElementCompositionPreview.GetElementVisual(panel);
        if (!AnimationsEnabled())
        {
            overlayVisual.Opacity = showing ? 1 : 0;
            panelVisual.Scale = showing ? System.Numerics.Vector3.One : new System.Numerics.Vector3(0.985f);
            return;
        }

        panel.UpdateLayout();
        panelVisual.CenterPoint = new System.Numerics.Vector3(
            (float)panel.ActualWidth / 2,
            (float)panel.ActualHeight / 2,
            0);
        Compositor compositor = overlayVisual.Compositor;
        TimeSpan duration = TimeSpan.FromMilliseconds(showing ? 210 : 260);
        CompositionEasingFunction easing = compositor.CreateCubicBezierEasingFunction(
            new System.Numerics.Vector2(0.1f, 0.9f),
            new System.Numerics.Vector2(0.2f, 1f));
        ScalarKeyFrameAnimation opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(0, showing ? 0 : 1);
        opacity.InsertKeyFrame(1, showing ? 1 : 0, easing);
        opacity.Duration = duration;
        Vector3KeyFrameAnimation scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0, showing ? new System.Numerics.Vector3(0.985f) : System.Numerics.Vector3.One);
        scale.InsertKeyFrame(1, showing ? System.Numerics.Vector3.One : new System.Numerics.Vector3(0.985f), easing);
        scale.Duration = duration;

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CompositionScopedBatch batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (_, _) => completion.TrySetResult();
        overlayVisual.StartAnimation("Opacity", opacity);
        panelVisual.StartAnimation("Scale", scale);
        batch.End();
        await completion.Task;
    }

    internal static void ApplyRoundedTextBoxResources(TextBox textBox)
    {
        // El botón interno de borrado usa un rectángulo propio que no respeta el
        // radio exterior. Conservamos su icono y eliminamos el relleno desbordado.
        SolidColorBrush transparent = new(Color.FromArgb(0, 0, 0, 0));
        textBox.Resources["TextControlButtonBackgroundPointerOver"] = transparent;
        textBox.Resources["TextControlButtonBackgroundPressed"] = transparent;
        textBox.Resources["TextControlButtonBorderBrushPointerOver"] = transparent;
        textBox.Resources["TextControlButtonBorderBrushPressed"] = transparent;
        ResourceDictionary light = new();
        light["TextControlButtonForegroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xE4, 0, 0, 0));
        light["TextControlButtonForegroundPressed"] = new SolidColorBrush(Color.FromArgb(0xE4, 0, 0, 0));
        ResourceDictionary dark = new();
        dark["TextControlButtonForegroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        dark["TextControlButtonForegroundPressed"] = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        ResourceDictionary fallback = new();
        fallback["TextControlButtonForegroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0xE4, 0, 0, 0));
        fallback["TextControlButtonForegroundPressed"] = new SolidColorBrush(Color.FromArgb(0xE4, 0, 0, 0));
        textBox.Resources.ThemeDictionaries["Light"] = light;
        textBox.Resources.ThemeDictionaries["Dark"] = dark;
        textBox.Resources.ThemeDictionaries["Default"] = fallback;
    }

    private static bool AnimationsEnabled()
    {
        try { return new UISettings().AnimationsEnabled; }
        catch { return false; }
    }

    private void SetBusy(bool busy)
    {
        DeviceList.IsEnabled = !busy;
        SetEditorEnabled(!busy && DeviceList.SelectedItem is not null);
    }

    private void SetEditorEnabled(bool enabled)
    {
        DeviceRow? row = SelectedRow;
        bool logicalPreference = enabled && row is not null && !row.IsGroupMember;
        AliasTextBox.IsEnabled = logicalPreference;
        LayoutComboBox.IsEnabled = logicalPreference && IgnoredCheckBox.IsChecked != true;
        IgnoredCheckBox.IsEnabled = logicalPreference && row is { IsGroup: false };
        IgnoredCheckBox.Visibility = row is { IsGroup: false, IsGroupMember: false }
            ? Visibility.Visible
            : Visibility.Collapsed;
        SaveButton.IsEnabled = logicalPreference;
        ForgetButton.IsEnabled = enabled && row is { IsGroup: false, IsGroupMember: false };
        GroupTargetComboBox.IsEnabled = logicalPreference && row is { Ignored: false };
        GroupButton.IsEnabled = GroupTargetComboBox.IsEnabled && GroupTargetComboBox.SelectedItem is DeviceRow;
        bool canGroup = row is not null && !row.IsGroupMember;
        GroupTargetComboBox.Visibility = canGroup ? Visibility.Visible : Visibility.Collapsed;
        GroupButton.Visibility = canGroup ? Visibility.Visible : Visibility.Collapsed;
        UngroupButton.IsEnabled = enabled && row?.IsGroupMember == true;
        UngroupButton.Visibility = row?.IsGroupMember == true ? Visibility.Visible : Visibility.Collapsed;
        GroupButtonsPanel.Visibility = canGroup || row?.IsGroupMember == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        DeviceDetailsPanel.Visibility = row is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private static class NativeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate nint SubclassProc(
            nint hwnd,
            uint message,
            nint wParam,
            nint lParam,
            nuint subclassId,
            nuint referenceData);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MinMaxInfo
        {
            internal Point Reserved;
            internal Point MaxSize;
            internal Point MaxPosition;
            internal Point MinTrackSize;
            internal Point MaxTrackSize;
        }

        [DllImport("comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowSubclass(
            nint hwnd,
            SubclassProc callback,
            nuint subclassId,
            nuint referenceData);

        [DllImport("comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveWindowSubclass(
            nint hwnd,
            SubclassProc callback,
            nuint subclassId);

        [DllImport("comctl32.dll")]
        internal static extern nint DefSubclassProc(nint hwnd, uint message, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        internal static extern uint GetDpiForWindow(nint hwnd);
    }
}

public sealed class DeviceRow
{
    internal DeviceRow(SettingsDevice device, SettingsLayout? layout)
    {
        Identity = device.Identity;
        TargetIdentity = device.Identity;
        GroupId = device.GroupId;
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

    internal DeviceRow(SettingsDevice device, string groupId)
    {
        Identity = device.Identity;
        TargetIdentity = device.Identity;
        GroupId = groupId;
        DisplayName = device.DetectedName;
        DetectedName = device.DetectedName;
        TechnicalId = device.TechnicalId;
        LastSeenUtc = device.LastSeenUtc;
        Connected = device.Connected;
        IsGroupMember = true;
        string state = device.Connected ? "Conectado" : "Desconectado";
        Summary = $"Identidad técnica · {state}";
        AccessibleName = $"{DisplayName}. {Summary}";
    }

    internal DeviceRow(
        SettingsDeviceGroup group,
        SettingsLayout? layout,
        IReadOnlyDictionary<string, SettingsDevice> devices)
    {
        Identity = group.Id;
        TargetIdentity = group.MemberIdentities.First();
        GroupId = group.Id;
        DisplayName = group.DisplayName;
        DetectedName = string.Empty;
        TechnicalId = string.Empty;
        SettingsDevice[] members = group.MemberIdentities
            .Where(devices.ContainsKey)
            .Select(identity => devices[identity])
            .ToArray();
        LastSeenUtc = members.Select(member => member.LastSeenUtc).DefaultIfEmpty().Max();
        Connected = members.Any(member => member.Connected);
        Layout = layout;
        IsGroup = true;
        MemberCount = members.Length;
        string state = Connected ? "Conectado" : "Desconectado";
        Summary = layout is null
            ? $"{MemberCount} identidades · {state}"
            : $"{MemberCount} identidades · {state} · {layout.Name}";
        AccessibleName = $"Grupo {DisplayName}. {Summary}";
    }

    public string Identity { get; }
    public string TargetIdentity { get; }
    public string? GroupId { get; }
    public string DisplayName { get; }
    public string DetectedName { get; }
    public string TechnicalId { get; }
    public DateTimeOffset LastSeenUtc { get; }
    public string Summary { get; }
    public string AccessibleName { get; }
    public bool Connected { get; }
    public bool Ignored { get; }
    public bool IsGroup { get; }
    public bool IsGroupMember { get; }
    public bool CanBeGroupTarget => !IsGroup && !IsGroupMember && !Ignored;
    public int MemberCount { get; }
    internal SettingsLayout? Layout { get; }

    public override string ToString() => IsGroup ? $"Grupo: {DisplayName}" : DisplayName;
}
