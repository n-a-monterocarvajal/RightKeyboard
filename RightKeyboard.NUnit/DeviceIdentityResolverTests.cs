using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class DeviceIdentityResolverTests
{
    private static readonly string[] HardwareIds = ["HID\\VID_046D&PID_C548", "HID_DEVICE_SYSTEM_KEYBOARD"];

    [Test]
    public void SameContainerOnDifferentPaths_KeepsIdentity()
    {
        Guid container = Guid.NewGuid();
        DeviceIdentityResolver.DeviceProperties properties = Properties(container, "HID\\A", "Logitech MX Keys");

        DeviceIdentityResolver.DeviceDescriptor first = DeviceIdentityResolver.BuildDescriptor("ruta-a", properties);
        DeviceIdentityResolver.DeviceDescriptor second = DeviceIdentityResolver.BuildDescriptor("ruta-b", properties);

        Assert.That(second.Identity, Is.EqualTo(first.Identity));
    }

    [Test]
    public void IdenticalKeyboardsWithDifferentContainers_AreDistinguishable()
    {
        DeviceIdentityResolver.DeviceDescriptor first = DeviceIdentityResolver.BuildDescriptor(
            "ruta-a",
            Properties(Guid.NewGuid(), "HID\\A", "Logitech MX Keys"));
        DeviceIdentityResolver.DeviceDescriptor second = DeviceIdentityResolver.BuildDescriptor(
            "ruta-b",
            Properties(Guid.NewGuid(), "HID\\B", "Logitech MX Keys"));

        Assert.Multiple(() =>
        {
            Assert.That(second.Identity, Is.Not.EqualTo(first.Identity));
            Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        });
    }

    [Test]
    public void ReorderedHardwareIds_KeepFingerprint()
    {
        DeviceIdentityResolver.DeviceDescriptor first = DeviceIdentityResolver.BuildDescriptor(
            "ruta-a",
            Properties(Guid.NewGuid(), "HID\\A", "Logitech Keyboard"));
        DeviceIdentityResolver.DeviceDescriptor second = DeviceIdentityResolver.BuildDescriptor(
            "ruta-b",
            Properties(Guid.NewGuid(), "HID\\B", "Logitech Keyboard") with
            {
                HardwareIds = HardwareIds.Reverse().ToArray()
            });

        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
    }

    [Test]
    public void Refresh_RetriesADeviceWhoseFirstLookupWasIncomplete()
    {
        int reads = 0;
        Guid container = Guid.NewGuid();
        DeviceIdentityResolver resolver = new(_ => ++reads == 1
            ? DeviceIdentityResolver.DeviceProperties.Empty
            : Properties(container, "HID\\A", "Logitech MX Keys"));

        DeviceIdentityResolver.DeviceDescriptor fallback = resolver.Resolve("ruta");
        resolver.Refresh(["ruta"]);
        DeviceIdentityResolver.DeviceDescriptor recovered = resolver.Resolve("ruta");

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Identity, Does.StartWith("device:"));
            Assert.That(recovered.Identity, Is.EqualTo($"container:{container:D}"));
            Assert.That(reads, Is.EqualTo(2));
        });
    }

    [Test]
    public void Refresh_DoesNotDegradeAKnownIdentityAfterATransientFailure()
    {
        bool fail = false;
        Guid container = Guid.NewGuid();
        DeviceIdentityResolver resolver = new(_ => fail
            ? DeviceIdentityResolver.DeviceProperties.Empty
            : Properties(container, "HID\\A", "Logitech MX Keys"));
        DeviceIdentityResolver.DeviceDescriptor known = resolver.Resolve("ruta");

        fail = true;
        resolver.Refresh(["ruta"]);

        Assert.That(resolver.Resolve("ruta"), Is.EqualTo(known));
    }

    [Test]
    public void MxMasterReportedByTheBus_IsClassifiedAsNonKeyboard()
    {
        DeviceIdentityResolver.DeviceDescriptor descriptor = DeviceIdentityResolver.BuildDescriptor(
            "ruta",
            Properties(Guid.NewGuid(), "HID\\MOUSE", "Logitech MX Master 3S"));

        Assert.That(descriptor.IsClearlyNonKeyboard, Is.True);
    }

    [Test]
    public void GenericLogitechHidChild_RemainsConservativelyAmbiguous()
    {
        DeviceIdentityResolver.DeviceDescriptor descriptor = DeviceIdentityResolver.BuildDescriptor(
            "ruta",
            Properties(Guid.NewGuid(), "HID\\MOUSE", "HID Keyboard Device"));

        Assert.That(descriptor.IsClearlyNonKeyboard, Is.False);
    }

    private static DeviceIdentityResolver.DeviceProperties Properties(
        Guid container,
        string instance,
        string busDescription) => new(
            container,
            instance,
            busDescription,
            "HID Keyboard Device",
            "HID Keyboard Device",
            "Logitech",
            HardwareIds);
}
