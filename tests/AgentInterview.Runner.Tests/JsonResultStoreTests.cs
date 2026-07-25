using System.Text.Json;
using AgentInterview.Core;

namespace AgentInterview.Runner.Tests;

public sealed class JsonResultStoreTests : IDisposable
{
    private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsyncWritesResultSchemaEnvelope()
    {
        var runId = Guid.NewGuid();
        var store = new JsonResultStore(_outputDirectory);

        await store.SaveAsync(CreateResult(runId), CancellationToken.None);

        var outputPath = Path.Combine(_outputDirectory, $"{runId:N}.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
        Assert.Equal(runId, document.RootElement.GetProperty("runId").GetGuid());
        Assert.Equal("coding.calculator-api", document.RootElement.GetProperty("interview").GetProperty("id").GetString());
        Assert.Equal("local", document.RootElement.GetProperty("candidate").GetProperty("provider").GetString());
        Assert.Equal("ungraded", document.RootElement.GetProperty("outcome").GetProperty("status").GetString());
        Assert.Equal("abc", document.RootElement.GetProperty("reproducibility").GetProperty("interviewHash").GetString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    private static InterviewRunResult CreateResult(Guid runId) =>
        new(
            runId,
            new InterviewRef("coding.calculator-api", "1.0.0"),
            new CandidateConfiguration("local", "noop", "example-noop", "1.0.0", "noop"),
            "ungraded",
            0,
            100,
            Array.Empty<GraderCaseResult>(),
            new UsageSummary(0, 0, 0, 0),
            new ExecutionSummary(1, 0, 0),
            new ReproducibilitySummary("abc", "def", "ghi", "runner", "environment"),
            Array.Empty<TraceEvent>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
}
