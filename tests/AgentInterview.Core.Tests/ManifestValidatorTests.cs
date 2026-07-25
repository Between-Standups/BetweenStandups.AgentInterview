using AgentInterview.Core;

namespace AgentInterview.Core.Tests;

public sealed class ManifestValidatorTests : IDisposable
{
    private readonly string _packageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public ManifestValidatorTests()
    {
        Directory.CreateDirectory(_packageDirectory);
        File.WriteAllText(Path.Combine(_packageDirectory, "prompt.md"), "Prompt");
        Directory.CreateDirectory(Path.Combine(_packageDirectory, "starter"));
        Directory.CreateDirectory(Path.Combine(_packageDirectory, "grader"));
        File.WriteAllText(Path.Combine(_packageDirectory, "grader", "AgentInterview.Grader.csproj"), "<Project />");
    }

    [Fact]
    public void ValidateAcceptsCompleteDeterministicManifest()
    {
        var result = ManifestValidator.Validate(CreateValidManifest(), _packageDirectory);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRejectsNetworkAccess()
    {
        var manifest = CreateValidManifest() with { NetworkAccess = true };

        var result = ManifestValidator.Validate(manifest, _packageDirectory);

        Assert.Contains("networkAccess must be false for deterministic interviews.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsCandidatePathsOutsidePackage()
    {
        var manifest = CreateValidManifest() with
        {
            Candidate = new CandidateSpec
            {
                Instructions = "../prompt.md",
                Workspace = "starter"
            }
        };

        var result = ManifestValidator.Validate(manifest, _packageDirectory);

        Assert.Contains("candidate.instructions must stay inside the interview package.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsMissingGraderProject()
    {
        File.Delete(Path.Combine(_packageDirectory, "grader", "AgentInterview.Grader.csproj"));

        var result = ManifestValidator.Validate(CreateValidManifest(), _packageDirectory);

        Assert.Contains("grading.command project points to a missing file: grader/AgentInterview.Grader.csproj.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsCommandWithoutProjectPath()
    {
        var manifest = CreateValidManifest() with
        {
            Grading = new GradingSpec
            {
                Command = "dotnet test",
                PassThreshold = 100,
                MaximumScore = 100
            }
        };

        var result = ManifestValidator.Validate(manifest, _packageDirectory);

        Assert.Contains("grading.command must reference a local grader project path.", result.Errors);
    }

    public void Dispose()
    {
        if (Directory.Exists(_packageDirectory))
        {
            Directory.Delete(_packageDirectory, recursive: true);
        }
    }

    private static InterviewManifest CreateValidManifest() =>
        new()
        {
            SchemaVersion = "1.0",
            Id = "coding.calculator-api",
            Version = "1.0.0",
            Title = "Implement a Calculator API",
            Category = "coding",
            Difficulty = "mid",
            Runtime = new RuntimeSpec
            {
                Language = "csharp",
                Framework = "net10.0"
            },
            Limits = new ResourceLimits
            {
                TimeoutSeconds = 900,
                MaxInputTokens = 25000,
                MaxOutputTokens = 15000,
                MaxToolCalls = 100
            },
            Candidate = new CandidateSpec
            {
                Instructions = "prompt.md",
                Workspace = "starter"
            },
            Grading = new GradingSpec
            {
                Command = "dotnet test grader/AgentInterview.Grader.csproj",
                PassThreshold = 100,
                MaximumScore = 100
            },
            NetworkAccess = false
        };
}
