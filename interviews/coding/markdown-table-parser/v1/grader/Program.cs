using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var source = File.Exists(Path.Combine(workspace, "MarkdownTableParser.cs")) ? File.ReadAllText(Path.Combine(workspace, "MarkdownTableParser.cs")) : "";
var cases = new[]
{
    Check("parser.implemented", !source.Contains("NotImplementedException", StringComparison.Ordinal), 20, "Parser still throws NotImplementedException."),
    Check("parser.rows", source.Contains("Split", StringComparison.Ordinal) && source.Contains("Dictionary", StringComparison.Ordinal), 20, "Parser should split rows and return dictionaries."),
    Check("parser.trim", source.Contains("Trim", StringComparison.Ordinal), 15, "Cells and headers must be trimmed."),
    Check("parser.escape", source.Contains("\\|", StringComparison.Ordinal) || source.Contains("escaped", StringComparison.OrdinalIgnoreCase), 20, "Escaped pipe handling is required."),
    Check("parser.malformed", source.Contains("FormatException", StringComparison.Ordinal) || source.Contains("ArgumentException", StringComparison.Ordinal), 25, "Malformed tables must throw a deterministic exception.")
};
Write(cases);

static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
