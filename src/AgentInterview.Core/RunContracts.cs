namespace AgentInterview.Core;

public sealed record RunWorkspace(string RootDirectory, string CandidateWorkspaceDirectory);

public sealed record CandidateRunRequest(
    InterviewPackage Package,
    RunWorkspace Workspace,
    string CandidateConfigurationPath);

public sealed record CandidateRunResult(
    bool Succeeded,
    IReadOnlyList<TraceEvent> Trace,
    UsageSummary Usage,
    int Retries,
    int ToolCalls,
    string? ErrorMessage);

public sealed record GraderRunRequest(InterviewPackage Package, RunWorkspace Workspace);

public sealed record GraderRunResult(
    bool Passed,
    int Score,
    int MaximumScore,
    IReadOnlyList<GraderCaseResult> Cases,
    string StandardOutput,
    string StandardError);

public sealed record GraderCaseResult(string Name, bool Passed, int Score, string? Message);

public sealed record InterviewRunRequest(
    InterviewRef Interview,
    string CandidateConfigurationPath,
    string OutputDirectory);

public sealed record InterviewRunResult(
    Guid RunId,
    InterviewRef Interview,
    string Status,
    int Score,
    int MaximumScore,
    UsageSummary Usage,
    ExecutionSummary Execution,
    ReproducibilitySummary Reproducibility,
    IReadOnlyList<TraceEvent> Trace,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);

public sealed record UsageSummary(
    long InputTokens,
    long OutputTokens,
    long CachedInputTokens,
    decimal EstimatedCostUsd);

public sealed record ExecutionSummary(long LatencyMs, int Retries, int ToolCalls);

public sealed record ReproducibilitySummary(
    string InterviewHash,
    string StarterHash,
    string GraderHash,
    string RunnerVersion,
    string EnvironmentImage);

public sealed record TraceEvent(DateTimeOffset Timestamp, string Source, string Message);

public sealed record ReportRequest(string ResultsDirectory, string OutputDirectory);
