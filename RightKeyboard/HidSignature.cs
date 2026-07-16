using System.Globalization;

namespace RightKeyboard;

/// <summary>
/// Firma HID parcial compuesta únicamente por tokens públicos no únicos del
/// device path (enumerador, VID, PID, interfaz, colección) más las capacidades
/// Raw Input. No identifica una unidad concreta: describe un modelo/interfaz,
/// por lo que nunca sustituye a la identidad ni a la huella. Se usa solo para
/// diagnóstico y para extender un ignorado manual a dispositivos débilmente
/// identificados (huella vacía).
/// </summary>
public sealed record HidSignature(
    string Enumerator,
    string VendorId,
    string ProductId,
    string? InterfaceNumber,
    string? CollectionNumber,
    KeyboardDeviceCapabilities? Capabilities)
{
    private const string Absent = "-";

    public static HidSignature? TryFromDevice(string devicePath, KeyboardDeviceCapabilities? capabilities)
    {
        ArgumentNullException.ThrowIfNull(devicePath);
        string? vendorId = ReadHexToken(devicePath, "VID_", 4);
        string? productId = ReadHexToken(devicePath, "PID_", 4);
        if (vendorId is null || productId is null)
        {
            return null;
        }

        return new HidSignature(
            ReadPathEnumerator(devicePath),
            vendorId,
            productId,
            ReadHexToken(devicePath, "MI_", 2),
            ReadHexToken(devicePath, "COL", 2),
            capabilities);
    }

    public string ToCanonicalString() => string.Join('|',
        $"enum:{Enumerator}",
        $"vid:{VendorId}",
        $"pid:{ProductId}",
        $"mi:{InterfaceNumber ?? Absent}",
        $"col:{CollectionNumber ?? Absent}",
        $"caps:{(Capabilities is null ? Absent : FormatCapabilities(Capabilities))}");

    public static bool TryParseCanonical(string? value, out HidSignature? signature)
    {
        signature = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] tokens = value.Split('|');
        if (tokens.Length != 6 ||
            !TryReadToken(tokens[0], "enum:", out string enumerator) || !IsValidEnumerator(enumerator) ||
            !TryReadToken(tokens[1], "vid:", out string vendorId) || !IsHexToken(vendorId, 4) ||
            !TryReadToken(tokens[2], "pid:", out string productId) || !IsHexToken(productId, 4) ||
            !TryReadToken(tokens[3], "mi:", out string interfaceNumber) || !IsOptionalHexToken(interfaceNumber, 2) ||
            !TryReadToken(tokens[4], "col:", out string collectionNumber) || !IsOptionalHexToken(collectionNumber, 2) ||
            !TryReadToken(tokens[5], "caps:", out string capabilitiesToken) ||
            !TryParseCapabilities(capabilitiesToken, out KeyboardDeviceCapabilities? capabilities, out bool capabilitiesAbsent))
        {
            return false;
        }

        signature = new HidSignature(
            enumerator,
            vendorId,
            productId,
            interfaceNumber == Absent ? null : interfaceNumber,
            collectionNumber == Absent ? null : collectionNumber,
            capabilitiesAbsent ? null : capabilities);
        return true;
    }

    internal static string? ReadHexToken(string value, string marker, int length)
    {
        int markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0 || markerIndex + marker.Length + length > value.Length) return null;
        string token = value.Substring(markerIndex + marker.Length, length);
        return token.All(Uri.IsHexDigit) ? token.ToUpperInvariant() : null;
    }

    internal static string ReadPathEnumerator(string value)
    {
        string normalized = value.TrimStart('\\', '?', '.');
        int separator = normalized.IndexOf('#');
        string candidate = (separator < 0 ? normalized : normalized[..separator]).ToUpperInvariant();
        return IsValidEnumerator(candidate) ? candidate : "OTRO";
    }

    internal static bool HasVirtualPathHint(string value) =>
        value.Contains("RDP_KBD", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("TERMSRV", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("VIRTUAL", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("VMBUS", StringComparison.OrdinalIgnoreCase);

    private static string FormatCapabilities(KeyboardDeviceCapabilities capabilities) => string.Join('.',
        capabilities.Type,
        capabilities.SubType,
        capabilities.KeyboardMode,
        capabilities.NumberOfFunctionKeys,
        capabilities.NumberOfIndicators,
        capabilities.NumberOfKeysTotal);

    private static bool TryReadToken(string value, string prefix, out string token)
    {
        if (value.StartsWith(prefix, StringComparison.Ordinal) && value.Length > prefix.Length)
        {
            token = value[prefix.Length..];
            return true;
        }

        token = string.Empty;
        return false;
    }

    private static bool IsValidEnumerator(string value) =>
        value.Length is > 0 and <= 16 && value.All(character =>
            char.IsAsciiDigit(character) || char.IsAsciiLetterUpper(character) || character is '_' or '-');

    private static bool IsHexToken(string value, int length) =>
        value.Length == length && value.All(character => char.IsAsciiDigit(character) || character is >= 'A' and <= 'F');

    private static bool IsOptionalHexToken(string value, int length) =>
        value == Absent || IsHexToken(value, length);

    private static bool TryParseCapabilities(
        string value,
        out KeyboardDeviceCapabilities? capabilities,
        out bool absent)
    {
        capabilities = null;
        absent = value == Absent;
        if (absent)
        {
            return true;
        }

        string[] parts = value.Split('.');
        uint[] numbers = new uint[6];
        if (parts.Length != numbers.Length)
        {
            return false;
        }

        for (int index = 0; index < numbers.Length; index++)
        {
            if (!uint.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index]))
            {
                return false;
            }
        }

        capabilities = new KeyboardDeviceCapabilities(
            numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);
        return true;
    }
}
