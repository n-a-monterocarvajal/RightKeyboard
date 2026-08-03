using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RightKeyboard.WinUI.Tests;

public sealed partial class UnitTestAppWindow : Window
{
    internal static UnitTestAppWindow ActiveTestWindow { get; private set; } = null!;
    internal Grid Host => VisualTreeHost;

    public UnitTestAppWindow()
    {
        InitializeComponent();
        ActiveTestWindow = this;
    }
}
