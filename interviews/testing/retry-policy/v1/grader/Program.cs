using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var testSource = string.Join("\n", Directory.Exists(workspace) ? Directory.EnumerateFiles(workspace, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText) : []);
var cases = new[]
{
    Check("tests.present", testSource.Contains("[Fact]", StringComparison.Ordinal) || testSource.Contains("[Theory]", StringComparison.Ordinal), 20, "Add xUnit facts or theories."),
    Check("tests.success-after-retry", testSource.Contains("attempt", StringComparison.OrdinalIgnoreCase) && testSource.Contains("2", StringComparison.Ordinal), 20, "Cover eventual success after retry."),
    Check("tests.exhausted", testSource.Contains("maxRetries", StringComparison.Ordinal) || testSource.Contains("ThrowsAsync", StringComparison.Ordinal), 20, "Cover exhausted retries."),
    Check("tests.non-retryable", testSource.Contains("isRetryable", StringComparison.Ordinal) || testSource.Contains("false", StringComparison.Ordinal), 20, "Cover non-retryable exceptions."),
    Check("tests.cancellation", testSource.Contains("CancellationTokenSource", StringComparison.Ordinal) || testSource.Contains("OperationCanceledException", StringComparison.Ordinal), 20, "Cover cancellation without real sleeps.")
};
Write(cases);

static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
