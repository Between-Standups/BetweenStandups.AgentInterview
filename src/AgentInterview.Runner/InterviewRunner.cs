using System.Diagnostics;
using System.Reflection;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class InterviewRunner : IInterviewRunner
{
    private readonly IInterviewCatalog _catalog;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ICandidateAdapter _candidateAdapter;
    private readonly IContentHasher _hasher;

    public InterviewRunner(
        IInterviewCatalog catalog,
        IWorkspaceManager workspaceManager,
        ICandidateAdapter candidateAdapter,
        IContentHasher hasher)
    {
        _catalog = catalog;
        _workspaceManager = workspaceManager;
        _candidateAdapter = candidateAdapter;
        _hasher = hasher;
    }

    public async Task<InterviewRunResult> RunAsync(InterviewRunRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var package = await _catalog.GetAsync(request.Interview, cancellationToken).ConfigureAwait(false);
        var manifestValidation = ManifestValidator.Validate(package.Manifest, package.PackageDirectory);
        if (!manifestValidation.IsValid)
        {
            throw new InvalidOperationException($"Interview '{request.Interview}' is invalid: {string.Join(" ", manifestValidation.Errors)}");
        }

        var candidateConfiguration = await CandidateConfigurationLoader
            .LoadAsync(request.CandidateConfigurationPath, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(candidateConfiguration.Adapter, "noop", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported candidate adapter '{candidateConfiguration.Adapter}'.");
        }

        var workspace = await _workspaceManager.CreateAsync(package, cancellationToken).ConfigureAwait(false);
        var candidateResult = await _candidateAdapter.ExecuteAsync(
            new CandidateRunRequest(package, workspace, request.CandidateConfigurationPath, candidateConfiguration),
            cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        var completedAt = DateTimeOffset.UtcNow;

        var result = new InterviewRunResult(
            RunId: Guid.NewGuid(),
            Interview: request.Interview,
            Candidate: candidateConfiguration,
            Status: candidateResult.Succeeded ? "ungraded" : "candidate_failed",
            Score: 0,
            MaximumScore: package.Manifest.Grading.MaximumScore,
            GraderResults: Array.Empty<GraderCaseResult>(),
            Usage: candidateResult.Usage,
            Execution: new ExecutionSummary(stopwatch.ElapsedMilliseconds, candidateResult.Retries, candidateResult.ToolCalls),
            Reproducibility: new ReproducibilitySummary(
                await _hasher.HashDirectoryAsync(package.PackageDirectory, cancellationToken).ConfigureAwait(false),
                await _hasher.HashDirectoryAsync(Path.Combine(package.PackageDirectory, package.Manifest.Candidate.Workspace), cancellationToken).ConfigureAwait(false),
                await _hasher.HashDirectoryAsync(Path.Combine(package.PackageDirectory, "grader"), cancellationToken).ConfigureAwait(false),
                typeof(InterviewRunner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
                Environment.Version.ToString()),
            Trace: candidateResult.Trace,
            StartedAt: startedAt,
            CompletedAt: completedAt);

        await new JsonResultStore(request.OutputDirectory).SaveAsync(result, cancellationToken).ConfigureAwait(false);
        return result;
    }
}
