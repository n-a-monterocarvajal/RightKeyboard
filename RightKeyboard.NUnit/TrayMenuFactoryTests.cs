using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class TrayMenuFactoryTests
{
    [Test]
    public void Create_ConservaOrdenYAlineacionDeLasAcciones()
    {
        using ContextMenuStrip menu = TrayMenuFactory.Create(
            () => { },
            () => { },
            () => { });

        ToolStripMenuItem[] actions = menu.Items.OfType<ToolStripMenuItem>().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(menu.Renderer, Is.TypeOf<ModernMenuRenderer>());
            Assert.That(actions.Select(item => item.Text), Is.EqualTo(
                new[] { "&Configuración", "&Limpiar preferencias", "&Salir" }));
            Assert.That(actions, Is.All.Matches<ToolStripMenuItem>(
                item => !item.AutoSize &&
                    item.Height == 36 &&
                    item.TextAlign == ContentAlignment.MiddleLeft));
            Assert.That(menu.Items[2], Is.TypeOf<ToolStripSeparator>());
        });
    }
}
