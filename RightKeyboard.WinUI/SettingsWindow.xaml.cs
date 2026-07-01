using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace RightKeyboard.WinUI;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsIpcClient client;
    private readonly ObservableCollection<DeviceRow> rows = [];
    private SettingsSnapshot? snapshot;

    internal SettingsWindow(SettingsIpcClient client)
    {
        this.client = client;
        InitializeComponent();
        DeviceList.ItemsSource = rows;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 680));
        Activated += OnActivated;
    }

    private DeviceRow? SelectedRow => DeviceList.SelectedItem as DeviceRow;

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnActivated;
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

    private async void SaveAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await SaveSelectedAsync();
        args.Handled = true;
    }

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
}
