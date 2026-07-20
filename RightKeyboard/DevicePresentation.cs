namespace RightKeyboard;

internal readonly record struct DevicePresentation(string State, bool Ignored, string? LayoutName)
{
    public string SecondaryText => string.Join(" · ", GetSummaryParts());

    public string GetListText(string displayName, int maximumNameLength = 30)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumNameLength, 2);
        string compactName = displayName.Length <= maximumNameLength
            ? displayName
            : $"{displayName[..(maximumNameLength - 1)].TrimEnd()}…";
        return $"{compactName}\r\n{SecondaryText}";
    }

    public string GetAccessibleName(string displayName) =>
        $"{displayName}. {string.Join(". ", GetSummaryParts())}.";

    public static DevicePresentation Create(bool connected, bool ignored, Layout? layout) =>
        Create(connected, ignored, layout?.Name);

    public static DevicePresentation Create(bool connected, bool ignored, string? layoutName) => new(
        connected ? "Conectado" : "Desconectado",
        ignored,
        layoutName);

    private IEnumerable<string> GetSummaryParts()
    {
        yield return State;
        if (Ignored)
        {
            yield return "Ignorado";
        }

        if (LayoutName is not null)
        {
            yield return LayoutName;
        }
        else if (!Ignored)
        {
            yield return "Sin distribución";
        }
    }
}
