namespace AgentInterview.Core;

public interface IInterviewCatalog
{
    Task<IReadOnlyList<InterviewSummary>> ListAsync(CancellationToken cancellationToken);

    Task<InterviewPackage> GetAsync(InterviewRef interviewRef, CancellationToken cancellationToken);
}

public interface IWorkspaceManager
{
    Task<RunWorkspace> CreateAsync(InterviewPackage package, CancellationToken cancellationToken);
}

public interface ICandidateAdapter
{
    Task<CandidateRunResult> ExecuteAsync(CandidateRunRequest request, CancellationToken cancellationToken);
}

public interface IInterviewRunner
{
    Task<InterviewRunResult> RunAsync(InterviewRunRequest request, CancellationToken cancellationToken);
}

public interface IGrader
{
    Task<GraderRunResult> GradeAsync(GraderRunRequest request, CancellationToken cancellationToken);
}

public interface IUsageCalculator
{
    UsageSummary Calculate(CandidateRunResult candidateResult);
}

public interface IContentHasher
{
    Task<string> HashDirectoryAsync(string directory, CancellationToken cancellationToken);
}

public interface IResultStore
{
    Task SaveAsync(InterviewRunResult result, CancellationToken cancellationToken);
}

public interface IReportGenerator
{
    Task GenerateAsync(ReportRequest request, CancellationToken cancellationToken);
}
