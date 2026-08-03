using NUnit.Framework;
using RightKeyboard.Win32;

namespace RightKeyboard.Tests;

public sealed class ForegroundLayoutRequestTests
{
    [TestCase("Progman")]
    [TestCase("WorkerW")]
    public void ClassifyForegroundWindowClass_DesktopClasses_AreShell(string className)
    {
        Assert.That(
            API.ClassifyForegroundWindowClass(className),
            Is.EqualTo(ForegroundTargetKind.DesktopShell));
    }

    [TestCase(null)]
    [TestCase("")]
    public void ClassifyForegroundWindowClass_MissingClass_IsUnknown(string? className)
    {
        Assert.That(
            API.ClassifyForegroundWindowClass(className),
            Is.EqualTo(ForegroundTargetKind.Unknown));
    }

    [TestCase("Notepad")]
    [TestCase("CabinetWClass")]
    public void ClassifyForegroundWindowClass_OtherClasses_AreNotReportedVerbatim(string className)
    {
        Assert.That(
            API.ClassifyForegroundWindowClass(className),
            Is.EqualTo(ForegroundTargetKind.Other));
    }
}
