using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class DeviceClassifierTests
{
    [TestCase("Logitech MX Master 3S")]
    [TestCase("Logitech MX Anywhere 3S")]
    [TestCase("USB Optical Mouse")]
    [TestCase("Ratón compatible con HID")]
    [TestCase("HID-compliant pointing device")]
    [TestCase("Precision Touchpad")]
    [TestCase("Trackball")]
    public void PointingDevice_IsClearlyNonKeyboard(string name)
    {
        Assert.That(DeviceClassifier.IsClearlyNonKeyboard(name), Is.True);
    }

    [TestCase("Logitech MX Keys Keyboard")]
    [TestCase("Teclado y mouse inalámbricos")]
    [TestCase("Gaming Keypad")]
    [TestCase("Dispositivo desconocido")]
    [TestCase("Logi Bolt Receiver")]
    [TestCase("Logitech MX Master 3S Keyboard")]
    public void KeyboardOrAmbiguousDevice_IsNotAutomaticallyExcluded(string name)
    {
        Assert.That(DeviceClassifier.IsClearlyNonKeyboard(name), Is.False);
    }
}
