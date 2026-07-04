using NUnit.Framework;

namespace RightKeyboard.Tests;

public sealed class VersionPresentationTests
{
    [TestCase("1.5.0-beta.6", "1.5.0 beta 6")]
    [TestCase("1.5.0-beta.5.2+abc123", "1.5.0 beta 5.2")]
    [TestCase("1.5.0", "1.5.0")]
    public void Format_ProducesReadableVersion(string input, string expected) =>
        Assert.That(VersionPresentation.Format(input), Is.EqualTo(expected));
}
