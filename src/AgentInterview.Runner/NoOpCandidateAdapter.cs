using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class NoOpCandidateAdapter : ICandidateAdapter
{
    public Task<CandidateRunResult> ExecuteAsync(CandidateRunRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<TraceEvent> trace =
        [
            new TraceEvent(DateTimeOffset.UtcNow, "candidate.noop", "No-op candidate adapter completed without modifying the workspace.")
        ];

        var result = new CandidateRunResult(
            Succeeded: true,
            Trace: trace,
            Usage: new UsageSummary(0, 0, 0, 0),
            Retries: 0,
            ToolCalls: 0,
            ErrorMessage: null);

        return Task.FromResult(result);
    }
}
