using NUnit.Framework;
using System.Windows.Forms;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class PreferenceResetServiceTests
{
    [Test]
    public void TryClear_NoModificaNadaCuandoSeCancela()
    {
        bool cleared = false;
        PreferenceResetService service = new(
            () => cleared = true,
            _ => DialogResult.Cancel,
            (_, _) => Assert.Fail("No debía mostrar un error."));

        bool result = service.TryClear();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(cleared, Is.False);
        });
    }

    [Test]
    public void TryClear_EjecutaElMismoBorradoTrasConfirmar()
    {
        int clearCount = 0;
        PreferenceResetService service = new(
            () => clearCount++,
            _ => DialogResult.OK,
            (_, _) => Assert.Fail("No debía mostrar un error."));

        bool result = service.TryClear();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(clearCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void TryClear_InformaElErrorSinDivergir()
    {
        Exception? shownError = null;
        InvalidOperationException expected = new("fallo de prueba");
        PreferenceResetService service = new(
            () => throw expected,
            _ => DialogResult.OK,
            (_, error) => shownError = error);

        bool result = service.TryClear();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(shownError, Is.SameAs(expected));
        });
    }
}
