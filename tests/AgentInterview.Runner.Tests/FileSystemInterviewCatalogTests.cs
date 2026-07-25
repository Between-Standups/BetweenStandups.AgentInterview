using AgentInterview.Core;
using AgentInterview.Runner;

namespace AgentInterview.Runner.Tests;

public sealed class FileSystemInterviewCatalogTests
{
    [Fact]
    public async Task ListAsyncDiscoversSeedCalculatorInterview()
    {
        var repositoryRoot = FindRepositoryRoot();
        var catalog = new FileSystemInterviewCatalog(repositoryRoot);

        var interviews = await catalog.ListAsync(CancellationToken.None);

        var interview = Assert.Single(interviews);
        Assert.Equal("coding.calculator-api", interview.Id);
        Assert.Equal("1.0.0", interview.Version);
    }

    [Fact]
    public async Task GetAsyncReturnsRequestedPackage()
    {
        var repositoryRoot = FindRepositoryRoot();
        var catalog = new FileSystemInterviewCatalog(repositoryRoot);

        var package = await catalog.GetAsync(new InterviewRef("coding.calculator-api", "1.0.0"), CancellationToken.None);

        Assert.Equal("Implement a Calculator API", package.Manifest.Title);
        Assert.True(Directory.Exists(package.PackageDirectory));
    }

    [Fact]
    public async Task GetAsyncRejectsDuplicateManifests()
    {
        using var repository = TemporaryInterviewRepository.Create();
        repository.WriteInterview("interviews/coding/calculator-api/v1", ValidManifestJson());
        repository.WriteInterview("interviews/duplicates/calculator-api/v1", ValidManifestJson());
        var catalog = new FileSystemInterviewCatalog(repository.RootDirectory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.GetAsync(new InterviewRef("coding.calculator-api", "1.0.0"), CancellationToken.None));

        Assert.Equal("Interview 'coding.calculator-api@1.0.0' has duplicate manifests.", exception.Message);
    }

    [Fact]
    public async Task GetAsyncRejectsSchemaInvalidManifest()
    {
        using var repository = TemporaryInterviewRepository.Create();
        repository.WriteInterview(
            "interviews/coding/calculator-api/v1",
            """
            {
              "schemaVersion": "1.0",
              "id": "coding.calculator-api"
            }
            """);
        var catalog = new FileSystemInterviewCatalog(repository.RootDirectory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.GetAsync(new InterviewRef("coding.calculator-api", "1.0.0"), CancellationToken.None));

        Assert.Contains("does not match the V1 schema", exception.Message, StringComparison.Ordinal);
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

    private static string ValidManifestJson() =>
        """
        {
          "schemaVersion": "1.0",
          "id": "coding.calculator-api",
          "version": "1.0.0",
          "title": "Implement a Calculator API",
          "category": "coding",
          "difficulty": "mid",
          "runtime": {
            "language": "csharp",
            "framework": "net10.0"
          },
          "limits": {
            "timeoutSeconds": 900,
            "maxInputTokens": 25000,
            "maxOutputTokens": 15000,
            "maxToolCalls": 100
          },
          "candidate": {
            "instructions": "prompt.md",
            "workspace": "starter"
          },
          "grading": {
            "command": "dotnet test grader/AgentInterview.Grader.csproj",
            "passThreshold": 100,
            "maximumScore": 100
          },
          "networkAccess": false
        }
        """;
}

internal sealed class TemporaryInterviewRepository : IDisposable
{
    private TemporaryInterviewRepository(string rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    public string RootDirectory { get; }

    public static TemporaryInterviewRepository Create()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        return new TemporaryInterviewRepository(rootDirectory);
    }

    public void WriteInterview(string relativeDirectory, string manifestJson)
    {
        var packageDirectory = Path.Combine(RootDirectory, relativeDirectory);
        Directory.CreateDirectory(packageDirectory);
        File.WriteAllText(Path.Combine(packageDirectory, "interview.json"), manifestJson);
        File.WriteAllText(Path.Combine(packageDirectory, "prompt.md"), "Prompt");
        Directory.CreateDirectory(Path.Combine(packageDirectory, "starter"));
        Directory.CreateDirectory(Path.Combine(packageDirectory, "grader"));
        File.WriteAllText(Path.Combine(packageDirectory, "grader", "AgentInterview.Grader.csproj"), "<Project />");
    }

    public void Dispose()
    {
        if (Directory.Exists(RootDirectory))
        {
            Directory.Delete(RootDirectory, recursive: true);
        }
    }
}
