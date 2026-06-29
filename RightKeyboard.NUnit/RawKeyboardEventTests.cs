using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class RawKeyboardEventTests
{
    [Test]
    public void KeyRelease_DoesNotStartMapping()
    {
        RawKeyboardEvent keyboardEvent = new(1, 0x41, 0x1E, 0x0001, 0x0101);

        Assert.Multiple(() =>
        {
            Assert.That(keyboardEvent.IsKeyDown, Is.False);
            Assert.That(keyboardEvent.CanStartMapping, Is.False);
        });
    }

    [TestCase(0x10)]
    [TestCase(0x11)]
    [TestCase(0x12)]
    [TestCase(0x5B)]
    [TestCase(0x5C)]
    [TestCase(0xA0)]
    [TestCase(0xA5)]
    public void Modifier_DoesNotStartMapping(int virtualKey)
    {
        RawKeyboardEvent keyboardEvent = new(1, checked((ushort)virtualKey), 0, 0, 0x0100);

        Assert.That(keyboardEvent.CanStartMapping, Is.False);
    }

    [Test]
    public void FakeKey_DoesNotStartMapping()
    {
        RawKeyboardEvent keyboardEvent = new(1, 0x00FF, 0, 0, 0x0100);

        Assert.That(keyboardEvent.CanStartMapping, Is.False);
    }

    [Test]
    public void RegularKeyDown_CanStartMapping()
    {
        RawKeyboardEvent keyboardEvent = new(1, 0x41, 0x1E, 0, 0x0100);

        Assert.That(keyboardEvent.CanStartMapping, Is.True);
    }
}
