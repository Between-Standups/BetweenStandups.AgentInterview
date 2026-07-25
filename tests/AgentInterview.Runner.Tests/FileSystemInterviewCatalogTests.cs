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
