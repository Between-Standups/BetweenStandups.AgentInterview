using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var testSource = string.Join("\n", Directory.Exists(workspace) ? Directory.EnumerateFiles(workspace, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText) : []);
var cases = new[]
{
    Check("tests.present", testSource.Contains("[Fact]", StringComparison.Ordinal) || testSource.Contains("[Theory]", StringComparison.Ordinal), 20, "Add xUnit facts or theories."),
    Check("tests.thresholds", testSource.Contains("100", StringComparison.Ordinal) && testSource.Contains("500", StringComparison.Ordinal), 20, "Cover the 100 and 500 subtotal thresholds."),
    Check("tests.loyalty", testSource.Contains("loyal", StringComparison.OrdinalIgnoreCase) || testSource.Contains("true", StringComparison.Ordinal), 20, "Cover loyal customer discount behavior."),
    Check("tests.invalid", testSource.Contains("Throws", StringComparison.Ordinal) && testSource.Contains("ArgumentOutOfRangeException", StringComparison.Ordinal), 20, "Cover invalid negative totals."),
    Check("tests.rounding", testSource.Contains("Midpoint", StringComparison.Ordinal) || testSource.Contains(".01", StringComparison.Ordinal) || testSource.Contains(".67", StringComparison.Ordinal), 20, "Cover deterministic rounding.")
};
Write(cases);

static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
