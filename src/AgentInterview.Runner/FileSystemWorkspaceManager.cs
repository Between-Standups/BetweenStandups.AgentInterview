using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class FileSystemWorkspaceManager : IWorkspaceManager
{
    private readonly string _workspaceRoot;

    public FileSystemWorkspaceManager(string? workspaceRoot = null)
    {
        _workspaceRoot = workspaceRoot ?? Path.Combine(Path.GetTempPath(), "agent-interview-runs");
    }

    public Task<RunWorkspace> CreateAsync(InterviewPackage package, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        cancellationToken.ThrowIfCancellationRequested();

        var runRoot = Path.Combine(_workspaceRoot, Guid.NewGuid().ToString("N"));
        var candidateWorkspace = Path.Combine(runRoot, "workspace");
        Directory.CreateDirectory(candidateWorkspace);

        var starterDirectory = Path.Combine(package.PackageDirectory, package.Manifest.Candidate.Workspace);
        CopyDirectory(starterDirectory, candidateWorkspace, cancellationToken);

        return Task.FromResult(new RunWorkspace(runRoot, candidateWorkspace));
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory, CancellationToken cancellationToken)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destination = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? destinationDirectory);
            File.Copy(file, destination, overwrite: false);
        }
    }
}
