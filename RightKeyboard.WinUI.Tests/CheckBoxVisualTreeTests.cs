using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using RightKeyboard;

namespace RightKeyboard.WinUI.Tests;

[TestClass]
public sealed class CheckBoxVisualTreeTests
{
    [UITestMethod]
    public async Task Produccion_AplicaRadioAlRectanguloRealDelGlifo()
    {
        CheckBox checkBox = new() { Content = "Ignorar eventos de este dispositivo" };
        ApplyProductionResources(checkBox);
        TaskCompletionSource loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        checkBox.Loaded += (_, _) => loaded.TrySetResult();
        Grid host = UnitTestAppWindow.ActiveTestWindow.Host;

        try
        {
            host.Children.Add(checkBox);
            await loaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
            host.UpdateLayout();
            checkBox.ApplyTemplate();
            host.UpdateLayout();

            Rectangle? glyph = FindDescendant<Rectangle>(checkBox, "NormalRectangle");
            Assert.IsNotNull(glyph, "No se encontró el Rectangle real del glifo de CheckBox.");
            Assert.AreEqual(checkBox.CornerRadius.TopLeft, glyph.RadiusX, 0.01);
            Assert.AreEqual(checkBox.CornerRadius.BottomRight, glyph.RadiusY, 0.01);
            Assert.IsGreaterThan(0d, glyph.RadiusX, "El glifo resuelto sigue teniendo ángulos rectos.");
            Assert.IsGreaterThan(0d, glyph.RadiusY, "El glifo resuelto sigue teniendo ángulos rectos.");
        }
        finally
        {
            host.Children.Clear();
        }
    }

    [UITestMethod]
    public void Filas_MuestranIndicadorDeConexionSinPerderElNombreAccesible()
    {
        SettingsDevice connectedDevice = new(
            "device:connected",
            "Teclado conectado",
            "HID Keyboard",
            "HID\\CONNECTED",
            DateTimeOffset.UnixEpoch,
            Connected: true,
            Ignored: false,
            LayoutIdentifier: null,
            GroupId: null);
        SettingsDevice disconnectedDevice = connectedDevice with
        {
            Identity = "device:disconnected",
            DisplayName = "Teclado desconectado",
            Connected = false
        };

        ListViewItem connectedItem = CreateDeviceItem(connectedDevice);
        ListViewItem disconnectedItem = CreateDeviceItem(disconnectedDevice);
        Grid themeHost = new() { RequestedTheme = ElementTheme.Light };
        themeHost.Children.Add(connectedItem);
        themeHost.Children.Add(disconnectedItem);
        Grid host = UnitTestAppWindow.ActiveTestWindow.Host;

        try
        {
            host.Children.Add(themeHost);
            host.UpdateLayout();
            Ellipse? connectedIndicator = FindDescendant<Ellipse>(connectedItem, "ConnectionIndicator");
            Ellipse? disconnectedIndicator = FindDescendant<Ellipse>(disconnectedItem, "ConnectionIndicator");

            Assert.IsNotNull(connectedIndicator);
            Assert.IsNotNull(disconnectedIndicator);
            Assert.IsTrue(connectedIndicator.Width <= 6d, "El punto no debe competir con el nombre.");
            Assert.AreNotEqual(
                ((SolidColorBrush)connectedIndicator.Fill).Color,
                ((SolidColorBrush)disconnectedIndicator.Fill).Color);
            Assert.IsTrue(
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(connectedItem)
                    .Contains("Conectado", StringComparison.Ordinal));
            Assert.IsTrue(
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(disconnectedItem)
                    .Contains("Desconectado", StringComparison.Ordinal));

            Grid stateLine = (Grid)VisualTreeHelper.GetParent(connectedIndicator);
            TextBlock stateText = stateLine.Children.OfType<TextBlock>().Single();
            Assert.AreEqual(0, Grid.GetColumn(connectedIndicator));
            Assert.AreEqual(1, Grid.GetColumn(stateText));
            Assert.IsTrue(stateText.Text.StartsWith("Conectado", StringComparison.Ordinal));

            themeHost.RequestedTheme = ElementTheme.Dark;
            host.UpdateLayout();
            Assert.AreNotEqual(
                ((SolidColorBrush)connectedIndicator.Fill).Color,
                ((SolidColorBrush)disconnectedIndicator.Fill).Color,
                "Los indicadores deben seguir distinguiéndose en tema oscuro.");
        }
        finally
        {
            host.Children.Clear();
        }
    }

    private static void ApplyProductionResources(CheckBox checkBox)
    {
        MethodInfo? method = typeof(SettingsWindow).GetMethod(
            "ApplyRoundedCheckBoxResources",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "No se encontró el helper de producción de CheckBox.");
        method.Invoke(null, [checkBox]);
    }

    private static ListViewItem CreateDeviceItem(SettingsDevice device)
    {
        Type? rowType = typeof(SettingsWindow).Assembly.GetType("RightKeyboard.WinUI.DeviceRow");
        Assert.IsNotNull(rowType);
        object? row = Activator.CreateInstance(
            rowType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [device, null],
            culture: null);
        Assert.IsNotNull(row);
        MethodInfo? method = typeof(SettingsWindow).GetMethod(
            "CreateDeviceItem",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (ListViewItem)method.Invoke(null, [row])!;
    }

    private static T? FindDescendant<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is T candidate && string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                return candidate;
            }

            T? nested = FindDescendant<T>(child, name);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
