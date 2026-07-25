using AgentInterview.Core;

namespace AgentInterview.Runner.Tests;

public sealed class FileSystemWorkspaceManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CreateAsyncCopiesStarterToCleanWorkspace()
    {
        var packageDirectory = Path.Combine(_root, "package");
        var starterDirectory = Path.Combine(packageDirectory, "starter");
        Directory.CreateDirectory(starterDirectory);
        File.WriteAllText(Path.Combine(starterDirectory, "README.md"), "starter");
        var workspaceRoot = Path.Combine(_root, "workspaces");
        var manager = new FileSystemWorkspaceManager(workspaceRoot);

        var workspace = await manager.CreateAsync(CreatePackage(packageDirectory), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(workspace.CandidateWorkspaceDirectory, "README.md")));
        Assert.StartsWith(workspaceRoot, workspace.RootDirectory, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static InterviewPackage CreatePackage(string packageDirectory) =>
        new(
            new InterviewManifest
            {
                Candidate = new CandidateSpec
                {
                    Instructions = "prompt.md",
                    Workspace = "starter"
                }
            },
            Path.Combine(packageDirectory, "interview.json"),
            packageDirectory);
}
