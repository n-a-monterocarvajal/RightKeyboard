using RightKeyboard.Win32;

namespace RightKeyboard;

public sealed class Layout
{
    public Layout(nint identifier, string name)
    {
        Identifier = identifier;
        Name = name;
    }

    public nint Identifier { get; }

    public string Name { get; }

    public override string ToString() => Name;

    public static IEnumerable<Layout> EnumerateLayouts() =>
        API.GetKeyboardLayouts()
            .Distinct()
            .Select(identifier => new Layout(identifier, API.GetKeyboardLayoutName(identifier)));
}
