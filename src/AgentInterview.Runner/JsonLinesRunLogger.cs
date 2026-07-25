using System.Text.Json;
using System.Text.Json.Serialization;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class JsonLinesRunLogger : IRunLogger
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _logPath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public JsonLinesRunLogger(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);
        _logPath = Path.Combine(outputDirectory, "run.log.jsonl");
    }

    public async Task LogAsync(
        string level,
        string eventName,
        IReadOnlyDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        var entry = new LogEntry(DateTimeOffset.UtcNow, level, eventName, properties);
        var line = JsonSerializer.Serialize(entry, SerializerOptions);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private sealed record LogEntry(
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
        [property: JsonPropertyName("level")] string Level,
        [property: JsonPropertyName("eventName")] string EventName,
        [property: JsonPropertyName("properties")] IReadOnlyDictionary<string, string> Properties);
}
