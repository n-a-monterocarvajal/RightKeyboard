using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

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

    private static void ApplyProductionResources(CheckBox checkBox)
    {
        MethodInfo? method = typeof(SettingsWindow).GetMethod(
            "ApplyRoundedCheckBoxResources",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "No se encontró el helper de producción de CheckBox.");
        method.Invoke(null, [checkBox]);
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
