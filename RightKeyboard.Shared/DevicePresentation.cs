namespace RightKeyboard;

internal readonly record struct DevicePresentation(
    bool Connected,
    string State,
    bool Ignored,
    string? LayoutName)
{
    public string SecondaryText => string.Join(" · ", GetSummaryParts());

    public int SortRank => GetSortRank(Connected, Ignored, LayoutName is not null);

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

    public static DevicePresentation Create(bool connected, bool ignored, string? layoutName) => new(
        connected,
        connected ? "Conectado" : "Desconectado",
        ignored,
        layoutName);

    public static DevicePresentation CreateGroup(IEnumerable<bool> connectedMembers, string? layoutName)
    {
        ArgumentNullException.ThrowIfNull(connectedMembers);
        return Create(connectedMembers.Any(connected => connected), ignored: false, layoutName);
    }

    public static int GetSortRank(bool connected, bool ignored, bool configured)
    {
        int connectionRank = connected ? 0 : 3;
        int logicalRank = ignored ? 2 : configured ? 0 : 1;
        return connectionRank + logicalRank;
    }

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
