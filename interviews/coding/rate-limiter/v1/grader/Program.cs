using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var source = File.Exists(Path.Combine(workspace, "FixedWindowRateLimiter.cs")) ? File.ReadAllText(Path.Combine(workspace, "FixedWindowRateLimiter.cs")) : "";
var cases = new[]
{
    Check("limiter.implemented", !source.Contains("NotImplementedException", StringComparison.Ordinal), 20, "Limiter still throws NotImplementedException."),
    Check("limiter.per-key", source.Contains("Dictionary", StringComparison.Ordinal) || source.Contains("ConcurrentDictionary", StringComparison.Ordinal), 20, "Limiter must track counts per key."),
    Check("limiter.thread-safe", source.Contains("lock", StringComparison.Ordinal) || source.Contains("ConcurrentDictionary", StringComparison.Ordinal), 20, "Limiter must be thread-safe."),
    Check("limiter.injected-time", !source.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal) && !source.Contains("DateTime.UtcNow", StringComparison.Ordinal), 20, "Allow must use the provided timestamp instead of wall-clock time."),
    Check("limiter.cleanup", source.Contains("Remove", StringComparison.Ordinal) || source.Contains("TryRemove", StringComparison.Ordinal), 20, "Expired window state should be cleaned up.")
};
Write(cases);

static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
