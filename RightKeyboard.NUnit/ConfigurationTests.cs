using NUnit.Framework;

namespace RightKeyboard.Tests;

[TestFixture]
public sealed class ConfigurationTests
{
    private string temporaryDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "RightKeyboard.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(temporaryDirectory, true);
    }

    [Test]
    public void Save_WritesStableDeviceIdentityAndLayout()
    {
        string path = Path.Combine(temporaryDirectory, "config.txt");
        Configuration configuration = new();
        configuration.LayoutMappings["container:00000000-0000-0000-0000-000000000001"] =
            new Layout(new nint(0x0000040A), "Español");

        configuration.Save(path);

        Assert.That(
            File.ReadAllText(path).Trim(),
            Is.EqualTo("container:00000000-0000-0000-0000-000000000001=000000000000040A"));
    }

    [Test]
    public void Clear_RemovesMappingsAndPersistsEmptyFile()
    {
        string path = Path.Combine(temporaryDirectory, "config.txt");
        Configuration configuration = new();
        configuration.LayoutMappings["device:test"] = new Layout(new nint(0x409), "Prueba");

        configuration.Clear(path);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.LayoutMappings, Is.Empty);
            Assert.That(File.ReadAllText(path), Is.Empty);
        });
    }
}
