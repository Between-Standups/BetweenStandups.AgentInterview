using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE") ?? "";
var text = File.Exists(Path.Combine(workspace, "runbook.md")) ? File.ReadAllText(Path.Combine(workspace, "runbook.md")) : "";
var cases = new[]
{
    Check("runbook.severity", Has(text, "severity", "impact"), 15, "Include severity classification and impact."),
    Check("runbook.detection", Has(text, "detect") || Has(text, "alert") || Has(text, "monitor"), 15, "Include detection signals."),
    Check("runbook.triage", Has(text, "triage") && Has(text, "latency"), 15, "Include latency triage steps."),
    Check("runbook.communication", Has(text, "communication") || Has(text, "status page") || Has(text, "customer"), 15, "Include communication steps."),
    Check("runbook.mitigation", Has(text, "mitigation") || Has(text, "rollback"), 15, "Include mitigation or rollback."),
    Check("runbook.escalation", Has(text, "escalation") || Has(text, "owner"), 10, "Include escalation or owner guidance."),
    Check("runbook.postmortem", Has(text, "postmortem") && Has(text, "verification"), 15, "Include postmortem and verification.")
};
Write(cases);
static bool Has(string text, params string[] terms) => terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
static Case Check(string name, bool passed, int score, string message) => new(name, passed, passed ? score : 0, passed ? null : message);
static void Write(IReadOnlyList<Case> cases) => Console.WriteLine(JsonSerializer.Serialize(new Result(cases.Sum(x => x.Score) == 100, cases.Sum(x => x.Score), 100, cases), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
public sealed record Result([property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("maximumScore")] int MaximumScore, [property: JsonPropertyName("cases")] IReadOnlyList<Case> Cases);
public sealed record Case([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("passed")] bool Passed, [property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("message")] string? Message);
