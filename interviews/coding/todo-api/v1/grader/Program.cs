using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var programPath = Path.Combine(workspace, "Program.cs");
var source = File.Exists(programPath) ? File.ReadAllText(programPath) : "";

var cases = new[]
{
    Check("todo.routes", source.Contains("MapGet(\"/todos\"", StringComparison.Ordinal) && source.Contains("MapPost(\"/todos\"", StringComparison.Ordinal) && source.Contains("MapPut(\"/todos/{id}\"", StringComparison.Ordinal) && source.Contains("MapDelete(\"/todos/{id}\"", StringComparison.Ordinal), 30, "Missing one or more required todo routes."),
    Check("todo.model", source.Contains("title", StringComparison.OrdinalIgnoreCase) && source.Contains("completed", StringComparison.OrdinalIgnoreCase), 15, "Todo response must include title and completed fields."),
    Check("todo.validation", source.Contains("BadRequest", StringComparison.Ordinal) && source.Contains("string.IsNullOrWhiteSpace", StringComparison.Ordinal), 20, "Empty or whitespace titles must return HTTP 400."),
    Check("todo.not-found", source.Contains("NotFound", StringComparison.Ordinal), 15, "Missing todo updates/deletes must return HTTP 404."),
    Check("todo.ids", source.Contains("nextId", StringComparison.OrdinalIgnoreCase) || source.Contains("Interlocked", StringComparison.Ordinal), 20, "Todo IDs must be deterministic and monotonic.")
};

Write(cases);

static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);

static void Write(IReadOnlyList<Case> cases)
{
    var score = cases.Sum(item => item.Score);
    Console.WriteLine(JsonSerializer.Serialize(new Result(score == 100, score, 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
}

public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
