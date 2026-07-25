using AgentInterview.Core;

namespace AgentInterview.Runner.Tests;

public sealed class InterviewRunnerTests : IDisposable
{
    private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RunAsyncExecutesNoOpCandidateAndWritesResult()
    {
        var repositoryRoot = FindRepositoryRoot();
        var runner = new InterviewRunner(
            new FileSystemInterviewCatalog(repositoryRoot),
            new FileSystemWorkspaceManager(),
            new NoOpCandidateAdapter(),
            new PassingGrader(),
            new DirectoryContentHasher());

        var result = await runner.RunAsync(
            new InterviewRunRequest(
                new InterviewRef("coding.calculator-api", "1.0.0"),
                Path.Combine(repositoryRoot, "configs", "example-agent.json"),
                _outputDirectory),
            CancellationToken.None);

        Assert.Equal("passed", result.Status);
        Assert.Equal("local", result.Candidate.Provider);
        Assert.NotEmpty(result.Reproducibility.InterviewHash);
        Assert.True(File.Exists(Path.Combine(_outputDirectory, $"{result.RunId:N}.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AgentInterview.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class PassingGrader : IGrader
    {
        public Task<GraderRunResult> GradeAsync(GraderRunRequest request, CancellationToken cancellationToken)
        {
            var result = new GraderRunResult(
                true,
                request.Package.Manifest.Grading.MaximumScore,
                request.Package.Manifest.Grading.MaximumScore,
                [new GraderCaseResult("fixture.pass", true, request.Package.Manifest.Grading.MaximumScore, null)],
                string.Empty,
                string.Empty);

            return Task.FromResult(result);
        }
    }
}
