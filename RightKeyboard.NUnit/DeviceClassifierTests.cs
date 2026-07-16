using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class DeviceClassifierTests
{

    [Test]
    public void EmptyUnknownDevice_IsLikelySyntheticInputSource()
    {
        KeyboardDevice device = new(
            @"\\?\UNKNOWN#DEVICE", 1, "device:test", "", "Teclado sin nombre", "Dispositivo TEST", false,
            new KeyboardDeviceCapabilities(0, 0, 0, 0, 0, 0));

        Assert.That(DeviceClassifier.IsLikelySyntheticInputSource(device), Is.True);
    }

    [Test]
    public void AcpiKeyboardWithEmptyCapabilities_IsNotSyntheticInputSource()
    {
        KeyboardDevice device = new(
            @"\\?\ACPI#PNP0303", 1, "device:test", "", "Teclado sin nombre", "Dispositivo TEST", false,
            new KeyboardDeviceCapabilities(0, 0, 0, 0, 0, 0));

        Assert.That(DeviceClassifier.IsLikelySyntheticInputSource(device), Is.False);
    }
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

    [TestCase("Logitech MX Master 3S", "mx master")]
    [TestCase("USB Optical Mouse", "mouse")]
    [TestCase("Precision Touchpad", "touchpad")]
    [TestCase("Ratón compatible con HID", "ratón")]
    public void DescribeNonKeyboardMatch_ReturnsMatchedTerm(string name, string expected)
    {
        Assert.That(DeviceClassifier.DescribeNonKeyboardMatch(name), Is.EqualTo(expected));
    }

    [TestCase("Logitech MX Keys Keyboard")]
    [TestCase("Dispositivo desconocido")]
    [TestCase("Logitech MX Master 3S Keyboard")]
    public void DescribeNonKeyboardMatch_ReturnsNullForKeyboardOrAmbiguous(string name)
    {
        Assert.That(DeviceClassifier.DescribeNonKeyboardMatch(name), Is.Null);
    }
}
