using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class SettingsDialogConnectionIndicatorTests
{
    [Test]
    public void RespaldoWinForms_DibujaIndicadoresDistintosEnLaLineaDeEstado()
    {
        using RadioButton connected = CreateIndicator(connected: true);
        using RadioButton disconnected = CreateIndicator(connected: false);
        using Bitmap connectedBitmap = Render(connected);
        using Bitmap disconnectedBitmap = Render(disconnected);

        int x = 12 * connected.DeviceDpi / 96;
        int diameter = Math.Max(5, connected.DeviceDpi / 20);
        int y = (connected.ClientSize.Height / 2) + (connected.Font.Height / 2);
        Color connectedColor = connectedBitmap.GetPixel(x + (diameter / 2), y);
        Color disconnectedColor = disconnectedBitmap.GetPixel(x + (diameter / 2), y);

        Assert.Multiple(() =>
        {
            Assert.That(connectedColor.G, Is.GreaterThan(connectedColor.R));
            Assert.That(connectedColor, Is.Not.EqualTo(disconnectedColor));
            Assert.That(connected.Text, Does.Contain("Conectado"));
            Assert.That(disconnected.Text, Does.Contain("Desconectado"));
        });
    }

    private static RadioButton CreateIndicator(bool connected)
    {
        Type? type = typeof(SettingsDialog).GetNestedType(
            "DeviceRadioButton",
            BindingFlags.NonPublic);
        Assert.That(type, Is.Not.Null);
        RadioButton? button = Activator.CreateInstance(
            type!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [connected, false],
            culture: null) as RadioButton;
        Assert.That(button, Is.Not.Null);
        button!.Size = new Size(280, 76);
        button.Appearance = Appearance.Button;
        button.FlatStyle = FlatStyle.Flat;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(32, 6, 12, 6);
        button.Text = connected
            ? "Teclado oficina\r\nConectado · Sin distribución"
            : "Teclado oficina\r\nDesconectado · Sin distribución";
        button.CreateControl();
        return button;
    }

    private static Bitmap Render(Control control)
    {
        Bitmap bitmap = new(control.Width, control.Height);
        control.DrawToBitmap(bitmap, control.ClientRectangle);
        return bitmap;
    }
}
