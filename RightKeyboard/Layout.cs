using RightKeyboard.Win32;

namespace RightKeyboard;

public sealed class Layout
{
    public Layout(nint identifier, string languageName, string layoutName)
    {
        Identifier = identifier;
        LanguageName = languageName;
        LayoutName = layoutName;
    }

    public nint Identifier { get; }

    public string LanguageName { get; }

    public string LayoutName { get; }

    public string Name => $"{LanguageName} / {LayoutName}";

    public override string ToString() => Name;

    public static IEnumerable<Layout> EnumerateLayouts()
    {
        foreach (nint identifier in API.GetKeyboardLayouts().Distinct())
        {
            (string languageName, string layoutName) = API.GetKeyboardLayoutDescription(identifier);
            yield return new Layout(identifier, languageName, layoutName);
        }
    }
}
