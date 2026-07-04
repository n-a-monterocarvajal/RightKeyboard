using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RightKeyboard;

internal sealed class DiagnosticLogger
{
    private const long MaxFileBytes = 512 * 1024;
    private const int MaxFiles = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object sync = new();
    private readonly string directory;
    private readonly string enabledMarker;
    private readonly string currentLog;

    internal DiagnosticLogger(string? directory = null)
    {
        this.directory = directory ?? GetDefaultDirectory();
        enabledMarker = Path.Combine(this.directory, "diagnostico-habilitado");
        currentLog = Path.Combine(this.directory, "rightkeyboard-diagnostico.log");
    }

    internal bool IsDetailedEnabled => File.Exists(enabledMarker);
    internal string DirectoryPath => directory;

    internal void SetDetailedEnabled(bool enabled)
    {
        lock (sync)
        {
            Directory.CreateDirectory(directory);
            if (enabled)
            {
                File.WriteAllText(enabledMarker, "El diagnóstico detallado está habilitado.");
                WriteCore("diagnostico_habilitado", null, null);
            }
            else
            {
                WriteCore("diagnostico_deshabilitado", null, null);
                File.Delete(enabledMarker);
            }
        }
    }

    internal void Write(string eventName, KeyboardDevice? device = null, object? details = null)
    {
        if (!IsDetailedEnabled)
        {
            return;
        }

        lock (sync)
        {
            WriteCore(eventName, device, details);
        }
    }

    internal static string Anonymize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("RightKeyboard/diagnostico/v1|" + value));
        return Convert.ToHexString(hash)[..16];
    }

    internal static string GetDefaultDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard", "logs");

    private void WriteCore(string eventName, KeyboardDevice? device, object? details)
    {
        Directory.CreateDirectory(directory);
        RotateIfNeeded();
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            eventName,
            processVersion = typeof(DiagnosticLogger).Assembly.GetName().Version?.ToString(),
            device = device is KeyboardDevice value ? new
            {
                identity = Anonymize(value.Identity),
                identityKind = value.Identity.Split(':', 2)[0],
                fingerprint = Anonymize(value.Fingerprint),
                path = Anonymize(value.DevicePath),
                vendorId = ReadHexToken(value.DevicePath, "VID_", 4),
                productId = ReadHexToken(value.DevicePath, "PID_", 4),
                interfaceNumber = ReadHexToken(value.DevicePath, "MI_", 2),
                collectionNumber = ReadHexToken(value.DevicePath, "COL", 2),
                clearlyNonKeyboard = value.IsClearlyNonKeyboard
            } : null,
            details
        };
        File.AppendAllText(currentLog, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, Encoding.UTF8);
    }

    private static string? ReadHexToken(string value, string marker, int length)
    {
        int markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0 || markerIndex + marker.Length + length > value.Length)
        {
            return null;
        }

        string token = value.Substring(markerIndex + marker.Length, length);
        return token.All(Uri.IsHexDigit) ? token.ToUpperInvariant() : null;
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(currentLog) || new FileInfo(currentLog).Length < MaxFileBytes)
        {
            return;
        }

        File.Delete(currentLog + $".{MaxFiles - 1}");
        for (int index = MaxFiles - 2; index >= 1; index--)
        {
            string source = currentLog + $".{index}";
            if (File.Exists(source))
            {
                File.Move(source, currentLog + $".{index + 1}");
            }
        }

        File.Move(currentLog, currentLog + ".1");
    }
}
