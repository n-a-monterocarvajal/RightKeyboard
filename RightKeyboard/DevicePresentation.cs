namespace RightKeyboard;

internal readonly record struct DevicePresentation(string State, string Mode)
{
    public string SecondaryText => $"{State} · {Mode}";

    public string GetListText(string displayName, int maximumNameLength = 30)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumNameLength, 2);
        string compactName = displayName.Length <= maximumNameLength
            ? displayName
            : $"{displayName[..(maximumNameLength - 1)].TrimEnd()}…";
        return $"{compactName}\r\n{SecondaryText}";
    }

    public string GetAccessibleName(string displayName) =>
        $"{displayName}. Estado: {State}. Preferencia: {Mode}.";

    public static DevicePresentation Create(bool connected, bool ignored, Layout? layout) => new(
        connected ? "Conectado" : "Desconectado",
        ignored ? "Ignorado" : layout?.Name ?? "Sin distribución");
}
