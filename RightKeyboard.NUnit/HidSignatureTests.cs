using NUnit.Framework;

namespace RightKeyboard.Tests;

public sealed class HidSignatureTests
{
    private static readonly KeyboardDeviceCapabilities BaseusCapabilities = new(81, 0, 1, 12, 3, 101);

    [Test]
    public void TryFromDevice_BaseusLikePath_ExtractsPublicTokens()
    {
        HidSignature? signature = HidSignature.TryFromDevice(
            @"\\?\HID#VID_2571&PID_4104&MI_00&Col01#8&2f2a3f0&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}",
            BaseusCapabilities);

        Assert.That(signature, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(signature!.Enumerator, Is.EqualTo("HID"));
            Assert.That(signature.VendorId, Is.EqualTo("2571"));
            Assert.That(signature.ProductId, Is.EqualTo("4104"));
            Assert.That(signature.InterfaceNumber, Is.EqualTo("00"));
            Assert.That(signature.CollectionNumber, Is.EqualTo("01"));
            Assert.That(signature.Capabilities, Is.EqualTo(BaseusCapabilities));
            Assert.That(signature.ToCanonicalString(),
                Is.EqualTo("enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.101"));
        });
    }

    [Test]
    public void TryFromDevice_LowercaseTokens_NormalizesToCanonicalUppercase()
    {
        HidSignature? signature = HidSignature.TryFromDevice(
            @"\\?\hid#vid_046d&pid_c548&mi_00#PRIVATE", capabilities: null);

        Assert.That(signature, Is.Not.Null);
        Assert.That(signature!.ToCanonicalString(),
            Is.EqualTo("enum:HID|vid:046D|pid:C548|mi:00|col:-|caps:-"));
    }

    [TestCase(@"\\?\ACPI#PNP0303#4&1bd7f811&0")]
    [TestCase(@"\\?\ROOT#RDP_KBD#0000")]
    [TestCase("")]
    public void TryFromDevice_WithoutVidPid_ReturnsNull(string path) =>
        Assert.That(HidSignature.TryFromDevice(path, BaseusCapabilities), Is.Null);

    [Test]
    public void CanonicalString_DistinguishesCollectionAndCapabilities()
    {
        string samePathOtherCollection = @"\\?\HID#VID_2571&PID_4104&MI_00&Col02#X";
        string basePath = @"\\?\HID#VID_2571&PID_4104&MI_00&Col01#X";

        string baseline = HidSignature.TryFromDevice(basePath, BaseusCapabilities)!.ToCanonicalString();
        Assert.Multiple(() =>
        {
            Assert.That(HidSignature.TryFromDevice(samePathOtherCollection, BaseusCapabilities)!.ToCanonicalString(),
                Is.Not.EqualTo(baseline));
            Assert.That(HidSignature.TryFromDevice(basePath, new KeyboardDeviceCapabilities(81, 0, 1, 12, 3, 102))!
                .ToCanonicalString(), Is.Not.EqualTo(baseline));
            Assert.That(HidSignature.TryFromDevice(basePath, capabilities: null)!.ToCanonicalString(),
                Is.Not.EqualTo(baseline));
        });
    }

    [TestCase(@"\\?\HID#VID_2571&PID_4104&MI_00&Col01#X", true)]
    [TestCase(@"\\?\HID#VID_046D&PID_C548#PRIVATE", false)]
    public void TryParseCanonical_RoundTripsFromDevice(string path, bool withCapabilities)
    {
        HidSignature original = HidSignature.TryFromDevice(path, withCapabilities ? BaseusCapabilities : null)!;

        Assert.That(HidSignature.TryParseCanonical(original.ToCanonicalString(), out HidSignature? parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(original));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("enum:HID|vid:2571|pid:4104|mi:00|col:01")]
    [TestCase("enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3")]
    [TestCase("enum:HID|vid:257|pid:4104|mi:00|col:01|caps:-")]
    [TestCase("enum:HID|vid:2571|pid:410G|mi:00|col:01|caps:-")]
    [TestCase("enum:hid|vid:2571|pid:4104|mi:00|col:01|caps:-")]
    [TestCase("enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:81.0.1.12.3.-5")]
    [TestCase("enum:|vid:2571|pid:4104|mi:-|col:-|caps:-")]
    [TestCase("vid:2571|pid:4104|mi:-|col:-|caps:-|enum:HID")]
    public void TryParseCanonical_RejectsMalformedValues(string? value)
    {
        Assert.That(HidSignature.TryParseCanonical(value, out HidSignature? parsed), Is.False);
        Assert.That(parsed, Is.Null);
    }

    [TestCase(@"\\?\HID#VID_046D&PID_C548#PRIVATE", "HID")]
    [TestCase(@"\\?\ROOT#RDP_KBD#0000", "ROOT")]
    [TestCase(@"\\?\ACPI#PNP0303#4", "ACPI")]
    public void ReadPathEnumerator_ReturnsOnlyNonUniqueFamily(string path, string expected) =>
        Assert.That(HidSignature.ReadPathEnumerator(path), Is.EqualTo(expected));

    [TestCase(@"\\?\ROOT#RDP_KBD#0000")]
    [TestCase(@"\\?\VMBUS#SYNTHETIC_KBD#PRIVATE")]
    public void HasVirtualPathHint_RecognizesKnownVirtualFamilies(string path) =>
        Assert.That(HidSignature.HasVirtualPathHint(path), Is.True);
}
