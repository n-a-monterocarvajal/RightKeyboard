using System.Reflection;

namespace RightKeyboard;

internal static class VersionPresentation
{
    internal static string Current => Format(
        typeof(VersionPresentation).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion);

    internal static string Format(string? informationalVersion)
    {
        string value = string.IsNullOrWhiteSpace(informationalVersion)
            ? "versión desconocida"
            : informationalVersion.Split('+', 2)[0];
        return value.Replace("-beta.", " beta ", StringComparison.OrdinalIgnoreCase);
    }
}
