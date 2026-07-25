using System.Text.Json;
using System.Text.Json.Serialization;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class JsonResultStore : IResultStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _outputDirectory;

    public JsonResultStore(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        _outputDirectory = outputDirectory;
    }

    public async Task SaveAsync(InterviewRunResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        Directory.CreateDirectory(_outputDirectory);

        var document = ResultDocument.FromResult(result);
        var outputPath = Path.Combine(_outputDirectory, $"{result.RunId:N}.json");
        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private sealed record ResultDocument(
        Guid RunId,
        InterviewDocument Interview,
        CandidateDocument Candidate,
        OutcomeDocument Outcome,
        UsageDocument Usage,
        ExecutionDocument Execution,
        ReproducibilityDocument Reproducibility,
        IReadOnlyList<TraceDocument> Trace,
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt)
    {
        public static ResultDocument FromResult(InterviewRunResult result) =>
            new(
                result.RunId,
                new InterviewDocument(result.Interview.Id, result.Interview.Version),
                new CandidateDocument(
                    result.Candidate.Provider,
                    result.Candidate.Model,
                    result.Candidate.AgentConfiguration,
                    result.Candidate.PromptVersion),
                new OutcomeDocument(
                    result.Status,
                    result.Score,
                    result.MaximumScore,
                    result.GraderResults.Select(item => new GraderResultDocument(item.Name, item.Passed, item.Score, item.Message)).ToArray()),
                new UsageDocument(
                    result.Usage.InputTokens,
                    result.Usage.OutputTokens,
                    result.Usage.CachedInputTokens,
                    result.Usage.EstimatedCostUsd),
                new ExecutionDocument(
                    result.Execution.LatencyMs,
                    result.Execution.Retries,
                    result.Execution.ToolCalls),
                new ReproducibilityDocument(
                    result.Reproducibility.InterviewHash,
                    result.Reproducibility.StarterHash,
                    result.Reproducibility.GraderHash,
                    result.Reproducibility.RunnerVersion,
                    result.Reproducibility.EnvironmentImage),
                result.Trace.Select(item => new TraceDocument(item.Timestamp, item.Source, item.Message)).ToArray(),
                result.StartedAt,
                result.CompletedAt);
    }

    private sealed record InterviewDocument(string Id, string Version);

    private sealed record CandidateDocument(string Provider, string Model, string AgentConfiguration, string PromptVersion);

    private sealed record OutcomeDocument(
        string Status,
        int Score,
        int MaximumScore,
        IReadOnlyList<GraderResultDocument> GraderResults);

    private sealed record GraderResultDocument(string Name, bool Passed, int Score, string? Message);

    private sealed record UsageDocument(long InputTokens, long OutputTokens, long CachedInputTokens, decimal EstimatedCostUsd);

    private sealed record ExecutionDocument(long LatencyMs, int Retries, int ToolCalls);

    private sealed record ReproducibilityDocument(
        string InterviewHash,
        string StarterHash,
        string GraderHash,
        string RunnerVersion,
        string EnvironmentImage);

    private sealed record TraceDocument(DateTimeOffset Timestamp, string Source, string Message);
}
