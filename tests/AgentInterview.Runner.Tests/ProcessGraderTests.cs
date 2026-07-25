using AgentInterview.Core;

namespace AgentInterview.Runner.Tests;

public sealed class ProcessGraderTests : IDisposable
{
    private readonly string _packageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public ProcessGraderTests()
    {
        Directory.CreateDirectory(_packageDirectory);
        Directory.CreateDirectory(Path.Combine(_packageDirectory, "starter"));
        Directory.CreateDirectory(Path.Combine(_packageDirectory, "grader"));
    }

    [Fact]
    public async Task GradeAsyncParsesPassingJsonOutput()
    {
        var result = await GradeAsync("success");

        Assert.True(result.Passed);
        Assert.Equal(100, result.Score);
        Assert.Equal("fixture.success", Assert.Single(result.Cases).Name);
    }

    [Fact]
    public async Task GradeAsyncParsesFailingJsonOutput()
    {
        var result = await GradeAsync("failure");

        Assert.False(result.Passed);
        Assert.Equal(25, result.Score);
        Assert.Equal("Expected failure.", Assert.Single(result.Cases).Message);
    }

    [Fact]
    public async Task GradeAsyncReturnsFailureForMalformedJson()
    {
        var result = await GradeAsync("malformed");

        Assert.False(result.Passed);
        Assert.Equal("grader.output", Assert.Single(result.Cases).Name);
        Assert.Contains("malformed JSON", result.Cases[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GradeAsyncReturnsFailureForNonZeroExitCode()
    {
        var result = await GradeAsync("nonzero");

        Assert.False(result.Passed);
        Assert.Equal("grader.process", Assert.Single(result.Cases).Name);
        Assert.Contains("code 7", result.Cases[0].Message, StringComparison.Ordinal);
        Assert.Contains("nonzero failure", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GradeAsyncKillsTimedOutProcess()
    {
        var result = await GradeAsync("timeout", timeoutSeconds: 1);

        Assert.False(result.Passed);
        Assert.Equal("grader.timeout", Assert.Single(result.Cases).Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_packageDirectory))
        {
            Directory.Delete(_packageDirectory, recursive: true);
        }
    }

    private async Task<GraderRunResult> GradeAsync(string mode, int timeoutSeconds = 30)
    {
        var fixtureAssembly = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "AgentInterview.GraderFixture",
            "bin",
            "Debug",
            "net10.0",
            "AgentInterview.GraderFixture.dll");
        var package = new InterviewPackage(
            new InterviewManifest
            {
                Id = "coding.fixture",
                Version = "1.0.0",
                Limits = new ResourceLimits
                {
                    TimeoutSeconds = timeoutSeconds,
                    MaxInputTokens = 1,
                    MaxOutputTokens = 1,
                    MaxToolCalls = 1
                },
                Candidate = new CandidateSpec
                {
                    Instructions = "prompt.md",
                    Workspace = "starter"
                },
                Grading = new GradingSpec
                {
                    Command = $"dotnet \"{fixtureAssembly}\" {mode}",
                    MaximumScore = 100,
                    PassThreshold = 100
                }
            },
            Path.Combine(_packageDirectory, "interview.json"),
            _packageDirectory);

        var grader = new ProcessGrader();
        return await grader.GradeAsync(
            new GraderRunRequest(package, new RunWorkspace(_packageDirectory, Path.Combine(_packageDirectory, "starter"))),
            CancellationToken.None);
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
}
