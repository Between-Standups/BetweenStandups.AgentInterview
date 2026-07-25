using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var text = File.Exists(Path.Combine(workspace, "design.md")) ? File.ReadAllText(Path.Combine(workspace, "design.md")) : "";
var cases = new[]
{
    Check("permissions.manifest", Has(text, "manifest", "permission"), 15, "Describe manifest-declared permissions."),
    Check("permissions.approval", Has(text, "approval", "mode", "user"), 15, "Describe user approval modes."),
    Check("permissions.least-privilege", Has(text, "least privilege") || Has(text, "scope"), 15, "Describe least privilege or scoping."),
    Check("permissions.audit", Has(text, "audit", "log"), 15, "Describe audit logging."),
    Check("permissions.revocation", Has(text, "revoke") || Has(text, "revocation"), 15, "Describe revocation."),
    Check("permissions.sandbox", Has(text, "sandbox", "escalation"), 15, "Describe sandbox boundaries and escalation."),
    Check("permissions.migration", Has(text, "migration") || Has(text, "compatibility"), 10, "Describe migration or compatibility.")
};
Write(cases);
static bool Has(string text, params string[] terms) => terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
