using NUnit.Framework;

namespace RightKeyboard.Tests;

public sealed class FrontendExitCodesTests
{
    [Test]
    public void Success_IsZero() =>
        Assert.That(FrontendExitCodes.Success, Is.EqualTo(0));

    [Test]
    public void StartupFailure_IsDistinguishableFromSuccess() =>
        Assert.That(FrontendExitCodes.StartupFailure, Is.Not.EqualTo(FrontendExitCodes.Success));

    [Test]
    public void ShouldFallBack_OnStartupFailure_IsTrue() =>
        Assert.That(FrontendExitCodes.ShouldFallBack(FrontendExitCodes.StartupFailure), Is.True);

    [Test]
    public void ShouldFallBack_OnSuccess_IsFalse() =>
        Assert.That(FrontendExitCodes.ShouldFallBack(FrontendExitCodes.Success), Is.False);

    // Un cierre normal, una salida abrupta del runtime tras la interacción
    // (0xE0434352, excepción administrada no controlada) o un valor arbitrario no
    // reabren el fallback: solo lo hace la señal explícita de fallo de arranque.
    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(259)]
    [TestCase(unchecked((int)0xE0434352))]
    public void ShouldFallBack_OnOtherExitCodes_IsFalse(int exitCode) =>
        Assert.That(FrontendExitCodes.ShouldFallBack(exitCode), Is.False);
}
