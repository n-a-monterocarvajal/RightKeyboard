using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class NativeTrayMenuTests
{
    [Test]
    public void Commands_ConservanOrdenYTextosEnEspañol()
    {
        Assert.That(NativeTrayMenu.Commands, Is.EqualTo(new[]
        {
            (TrayMenuCommand.Settings, "Configuración"),
            (TrayMenuCommand.Exit, "Salir")
        }));
    }
}
