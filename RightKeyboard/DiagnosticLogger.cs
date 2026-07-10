using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace RightKeyboard;

internal sealed class DiagnosticLogger : IDisposable
{
    private const long MaxFileBytes = 512 * 1024;
    private const int MaxFiles = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string directory;
    private readonly string enabledMarker;
    private readonly string currentLog;
    private readonly Channel<QueueItem> queue;
    private readonly Task writerTask;
    private volatile bool detailedEnabled;
    private bool disposed;

    internal static bool IsAvailable
    {
        get
        {
#if RIGHTKEYBOARD_DIAGNOSTICS
            return true;
#else
            return false;
#endif
        }
    }

    internal DiagnosticLogger(string? directory = null, bool forceEnableForTests = false)
    {
        if (!IsAvailable && !forceEnableForTests)
        {
            throw new InvalidOperationException(
                "El diagnóstico detallado no está disponible en esta compilación.");
        }

        this.directory = directory ?? GetDefaultDirectory();
        enabledMarker = Path.Combine(this.directory, "diagnostico-habilitado");
        currentLog = Path.Combine(this.directory, "rightkeyboard-diagnostico.log");
        detailedEnabled = File.Exists(enabledMarker);
        queue = Channel.CreateBounded<QueueItem>(new BoundedChannelOptions(2048)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });
        writerTask = Task.Run(WriteLoopAsync);
    }

    internal bool IsDetailedEnabled => detailedEnabled;
    internal string DirectoryPath => directory;

    internal void SetDetailedEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Directory.CreateDirectory(directory);
        if (enabled)
        {
            File.WriteAllText(enabledMarker, "El diagnóstico detallado está habilitado.");
            detailedEnabled = true;
            Enqueue("diagnostico_habilitado", null, null);
        }
        else
        {
            Enqueue("diagnostico_deshabilitado", null, null);
            detailedEnabled = false;
            File.Delete(enabledMarker);
        }
    }

    internal void Write(string eventName, KeyboardDevice? device = null, object? details = null)
    {
        if (!disposed && IsDetailedEnabled)
        {
            Enqueue(eventName, device, details);
        }
    }

    internal async Task FlushAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await queue.Writer.WriteAsync(new QueueItem(null, completion));
        await completion.Task;
    }

    internal static string Anonymize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("RightKeyboard/diagnostico/v1|" + value));
        return Convert.ToHexString(hash)[..16];
    }

    internal static string GetDefaultDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard", "logs");

    private void Enqueue(string eventName, KeyboardDevice? device, object? details)
    {
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
                pathEnumerator = ReadPathEnumerator(value.DevicePath),
                virtualPathHint = HasVirtualPathHint(value.DevicePath),
                capabilities = value.Capabilities,
                clearlyNonKeyboard = value.IsClearlyNonKeyboard
            } : null,
            details
        };
        queue.Writer.TryWrite(new QueueItem(JsonSerializer.Serialize(entry, JsonOptions), null));
    }

    private async Task WriteLoopAsync()
    {
        List<QueueItem> batch = new();
        await foreach (QueueItem first in queue.Reader.ReadAllAsync())
        {
            batch.Add(first);
            await Task.Delay(100);
            while (queue.Reader.TryRead(out QueueItem? item)) batch.Add(item);

            string[] lines = batch.Where(item => item.Line is not null).Select(item => item.Line!).ToArray();
            try
            {
                if (lines.Length > 0)
                {
                    Directory.CreateDirectory(directory);
                    RotateIfNeeded();
                    File.AppendAllText(currentLog, string.Join(Environment.NewLine, lines) + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (IOException)
            {
                // El diagnóstico nunca debe interrumpir la función principal.
            }
            catch (UnauthorizedAccessException)
            {
                // El diagnóstico nunca debe interrumpir la función principal.
            }
            finally
            {
                foreach (QueueItem item in batch) item.FlushCompletion?.TrySetResult();
            }
            batch.Clear();
        }
    }

    private static string? ReadHexToken(string value, string marker, int length)
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
        return candidate.Length is > 0 and <= 16 && candidate.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '-')
            ? candidate
            : "OTRO";
    }

    internal static bool HasVirtualPathHint(string value) =>
        value.Contains("RDP_KBD", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("TERMSRV", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("VIRTUAL", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("VMBUS", StringComparison.OrdinalIgnoreCase);

    private void RotateIfNeeded()
    {
        if (!File.Exists(currentLog) || new FileInfo(currentLog).Length < MaxFileBytes) return;
        File.Delete(currentLog + $".{MaxFiles - 1}");
        for (int index = MaxFiles - 2; index >= 1; index--)
        {
            string source = currentLog + $".{index}";
            if (File.Exists(source)) File.Move(source, currentLog + $".{index + 1}");
        }
        File.Move(currentLog, currentLog + ".1");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        queue.Writer.TryComplete();
        try { writerTask.Wait(TimeSpan.FromSeconds(2)); }
        catch (AggregateException) { }
    }

    private sealed record QueueItem(string? Line, TaskCompletionSource? FlushCompletion);
}
