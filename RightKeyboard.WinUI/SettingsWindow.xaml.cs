using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace RightKeyboard.WinUI;

public sealed partial class SettingsWindow : Window
{
    private readonly Configuration configuration;
    private readonly KeyboardDevicesCollection devices;
    private readonly Layout[] layouts;
    private readonly ObservableCollection<DeviceRow> rows = [];

    public SettingsWindow(Configuration configuration, KeyboardDevicesCollection devices)
    {
        this.configuration = configuration;
        this.devices = devices;
        layouts = Layout.EnumerateLayouts()
            .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        InitializeComponent();
        DeviceList.ItemsSource = rows;
        LayoutComboBox.Items.Add("Sin distribución");
        foreach (Layout layout in layouts)
        {
            LayoutComboBox.Items.Add(layout);
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 680));
        RefreshRows();
    }

    private DeviceRow? SelectedRow => DeviceList.SelectedItem as DeviceRow;

    private void RefreshRows(string? identityToSelect = null)
    {
        devices.Refresh();
        HashSet<string> connected = devices.Select(device => device.Identity)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (KeyboardDevice device in devices)
        {
            configuration.TouchDevice(device);
        }

        rows.Clear();
        foreach (DevicePreference preference in configuration.Devices.Values
                     .OrderBy(preference => preference.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            bool ignored = configuration.IgnoredDevices.Contains(preference.Identity);
            configuration.LayoutMappings.TryGetValue(preference.Identity, out Layout? layout);
            rows.Add(new DeviceRow(preference, connected.Contains(preference.Identity), ignored, layout));
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

        DevicePreference preference = configuration.Devices[row.Identity];
        AliasTextBox.Text = preference.CustomName ?? preference.DisplayName;
        DetectedNameText.Text = $"Detectado: {preference.DetectedName}";
        TechnicalIdText.Text = $"Identificador: {preference.TechnicalId}";
        StatusText.Text = $"Estado: {(row.Connected ? "Conectado" : "Desconectado")} · Última detección: {preference.LastSeenUtc.ToLocalTime():g}";
        IgnoredCheckBox.IsChecked = row.Ignored;
        LayoutComboBox.SelectedItem = row.Layout is null
            ? LayoutComboBox.Items[0]
            : layouts.FirstOrDefault(candidate => candidate.Identifier == row.Layout.Identifier) ?? row.Layout;
        SetEditorEnabled(true);
    }

    private void IgnoredCheckBox_Changed(object sender, RoutedEventArgs e) =>
        LayoutComboBox.IsEnabled = SaveButton.IsEnabled && IgnoredCheckBox.IsChecked != true;

    private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveSelected();

    private void SaveAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SaveSelected();
        args.Handled = true;
    }

    private void SaveSelected()
    {
        if (SelectedRow is not DeviceRow row)
        {
            return;
        }

        configuration.UpdatePreference(
            row.Identity,
            AliasTextBox.Text,
            LayoutComboBox.SelectedItem as Layout,
            IgnoredCheckBox.IsChecked == true);
        configuration.Save();
        RefreshRows(row.Identity);
    }

    private async void ForgetButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is not DeviceRow row)
        {
            return;
        }

        ContentDialog confirmation = Confirmation(
            "Olvidar dispositivo",
            $"Se olvidarán el nombre y la preferencia de \"{row.DisplayName}\".",
            "Olvidar");
        if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        configuration.Forget(row.Identity);
        configuration.Save();
        RefreshRows();
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

        configuration.Clear();
        rows.Clear();
        AliasTextBox.Text = string.Empty;
        DetectedNameText.Text = string.Empty;
        TechnicalIdText.Text = string.Empty;
        StatusText.Text = "Las preferencias se limpiaron correctamente.";
        IgnoredCheckBox.IsChecked = false;
        LayoutComboBox.SelectedIndex = 0;
        SetEditorEnabled(false);
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e) => RefreshRows(SelectedRow?.Identity);

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
    public DeviceRow(DevicePreference preference, bool connected, bool ignored, Layout? layout)
    {
        Identity = preference.Identity;
        DisplayName = preference.DisplayName;
        Connected = connected;
        Ignored = ignored;
        Layout = layout;
        DevicePresentation presentation = DevicePresentation.Create(connected, ignored, layout);
        Summary = presentation.SecondaryText;
        AccessibleName = presentation.GetAccessibleName(DisplayName);
    }

    public string Identity { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public string AccessibleName { get; }
    public bool Connected { get; }
    public bool Ignored { get; }
    public Layout? Layout { get; }
}
