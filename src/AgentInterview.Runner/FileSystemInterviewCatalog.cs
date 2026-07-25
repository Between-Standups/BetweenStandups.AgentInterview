using System.Text.Json;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class FileSystemInterviewCatalog : IInterviewCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _interviewsDirectory;

    public FileSystemInterviewCatalog(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        _interviewsDirectory = Path.Combine(repositoryRoot, "interviews");
    }

    public async Task<IReadOnlyList<InterviewSummary>> ListAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_interviewsDirectory))
        {
            return Array.Empty<InterviewSummary>();
        }

        var summaries = new List<InterviewSummary>();
        foreach (var manifestPath in Directory.EnumerateFiles(_interviewsDirectory, "interview.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var package = await LoadPackageAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            summaries.Add(new InterviewSummary(
                package.Manifest.Id,
                package.Manifest.Version,
                package.Manifest.Title,
                package.Manifest.Category,
                package.Manifest.Difficulty,
                manifestPath));
        }

        return summaries;
    }

    public async Task<InterviewPackage> GetAsync(InterviewRef interviewRef, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(interviewRef);

        var summaries = await ListAsync(cancellationToken).ConfigureAwait(false);
        var matches = summaries
            .Where(summary => string.Equals(summary.Id, interviewRef.Id, StringComparison.Ordinal)
                && string.Equals(summary.Version, interviewRef.Version, StringComparison.Ordinal))
            .ToList();

        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"Interview '{interviewRef}' was not found."),
            1 => await LoadPackageAsync(matches[0].ManifestPath, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Interview '{interviewRef}' has duplicate manifests.")
        };
    }

    private static async Task<InterviewPackage> LoadPackageAsync(string manifestPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<InterviewManifest>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        if (manifest is null)
        {
            throw new InvalidOperationException($"Manifest '{manifestPath}' could not be read.");
        }

        return new InterviewPackage(
            manifest,
            manifestPath,
            Path.GetDirectoryName(manifestPath) ?? throw new InvalidOperationException($"Manifest '{manifestPath}' has no parent directory."));
    }
}
