using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var text = File.Exists(Path.Combine(workspace, "design.md")) ? File.ReadAllText(Path.Combine(workspace, "design.md")) : "";
var cases = new[]
{
    Check("design.api", Has(text, "api", "schema", "validation"), 15, "Describe API shape, schema, and validation."),
    Check("design.storage", Has(text, "storage", "partition", "retention"), 15, "Describe storage, partitioning, and retention."),
    Check("design.idempotency", Has(text, "idempot", "dedup"), 15, "Describe idempotency and deduplication."),
    Check("design.backpressure", Has(text, "backpressure", "queue", "rate"), 15, "Describe backpressure/rate behavior."),
    Check("design.failure", Has(text, "retry", "dead-letter", "failure"), 15, "Describe failure handling."),
    Check("design.observability", Has(text, "metric", "log", "trace", "alert"), 15, "Describe observability."),
    Check("design.security", Has(text, "tenant", "auth", "encrypt"), 10, "Describe multi-tenant security.")
};
Write(cases);
static bool Has(string text, params string[] terms) => terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
