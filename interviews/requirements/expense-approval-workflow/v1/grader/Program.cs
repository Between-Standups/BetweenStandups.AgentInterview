using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var text = File.Exists(Path.Combine(workspace, "requirements.md")) ? File.ReadAllText(Path.Combine(workspace, "requirements.md")) : "";
var cases = new[]
{
    Check("requirements.summary", Has(text, "summary") || Has(text, "goal"), 10, "Include a summary or goals."),
    Check("requirements.actors", Has(text, "employee", "manager", "finance"), 15, "Identify employee, manager, and finance actors."),
    Check("requirements.workflow", Has(text, "submit", "approve", "reimburse"), 15, "Describe the submission, approval, and reimbursement workflow."),
    Check("requirements.rules", Has(text, "threshold") || Has(text, "amount"), 15, "Capture amount thresholds or routing rules."),
    Check("requirements.acceptance", Has(text, "acceptance criteria") || Has(text, "given"), 15, "Include acceptance criteria."),
    Check("requirements.questions", Has(text, "open questions") || Has(text, "question"), 15, "List open questions."),
    Check("requirements.nonfunctional", Has(text, "audit") && (Has(text, "security") || Has(text, "privacy")), 15, "Include audit and security/privacy needs.")
};
Write(cases);
static bool Has(string text, params string[] terms) => terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
