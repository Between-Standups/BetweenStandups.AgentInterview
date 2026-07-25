using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class InterviewRunner : IInterviewRunner
{
    private readonly IInterviewCatalog _catalog;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ICandidateAdapter _candidateAdapter;
    private readonly IGrader _grader;
    private readonly IContentHasher _hasher;
    private readonly IRunLogger _logger;

    public InterviewRunner(
        IInterviewCatalog catalog,
        IWorkspaceManager workspaceManager,
        ICandidateAdapter candidateAdapter,
        IGrader grader,
        IContentHasher hasher,
        IRunLogger? logger = null)
    {
        _catalog = catalog;
        _workspaceManager = workspaceManager;
        _candidateAdapter = candidateAdapter;
        _grader = grader;
        _hasher = hasher;
        _logger = logger ?? new NullRunLogger();
    }

    public async Task<InterviewRunResult> RunAsync(InterviewRunRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        await _logger.LogAsync(
            "info",
            "run.started",
            new Dictionary<string, string>
            {
                ["interview"] = request.Interview.ToString(),
                ["outputDirectory"] = request.OutputDirectory
            },
            cancellationToken).ConfigureAwait(false);

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
        await _logger.LogAsync(
            "info",
            "workspace.created",
            new Dictionary<string, string>
            {
                ["rootDirectory"] = workspace.RootDirectory,
                ["candidateWorkspaceDirectory"] = workspace.CandidateWorkspaceDirectory
            },
            cancellationToken).ConfigureAwait(false);

        var candidateResult = await _candidateAdapter.ExecuteAsync(
            new CandidateRunRequest(package, workspace, request.CandidateConfigurationPath, candidateConfiguration),
            cancellationToken).ConfigureAwait(false);
        await _logger.LogAsync(
            "info",
            "candidate.completed",
            new Dictionary<string, string>
            {
                ["succeeded"] = candidateResult.Succeeded.ToString(),
                ["toolCalls"] = candidateResult.ToolCalls.ToString(CultureInfo.InvariantCulture),
                ["retries"] = candidateResult.Retries.ToString(CultureInfo.InvariantCulture)
            },
            cancellationToken).ConfigureAwait(false);

        var graderResult = candidateResult.Succeeded
            ? await _grader.GradeAsync(new GraderRunRequest(package, workspace), cancellationToken).ConfigureAwait(false)
            : new GraderRunResult(
                false,
                0,
                package.Manifest.Grading.MaximumScore,
                [new GraderCaseResult("candidate.execution", false, 0, candidateResult.ErrorMessage)],
                string.Empty,
                candidateResult.ErrorMessage ?? string.Empty);
        await _logger.LogAsync(
            "info",
            "grader.completed",
            new Dictionary<string, string>
            {
                ["passed"] = graderResult.Passed.ToString(),
                ["score"] = graderResult.Score.ToString(CultureInfo.InvariantCulture),
                ["maximumScore"] = graderResult.MaximumScore.ToString(CultureInfo.InvariantCulture)
            },
            cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        var completedAt = DateTimeOffset.UtcNow;

        var result = new InterviewRunResult(
            RunId: Guid.NewGuid(),
            Interview: request.Interview,
            Candidate: candidateConfiguration,
            Status: candidateResult.Succeeded
                ? graderResult.Passed ? "passed" : "failed"
                : "candidate_failed",
            Score: graderResult.Score,
            MaximumScore: graderResult.MaximumScore,
            GraderResults: graderResult.Cases,
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
        await _logger.LogAsync(
            "info",
            "result.saved",
            new Dictionary<string, string>
            {
                ["runId"] = result.RunId.ToString("N"),
                ["status"] = result.Status
            },
            cancellationToken).ConfigureAwait(false);

        return result;
    }
}
