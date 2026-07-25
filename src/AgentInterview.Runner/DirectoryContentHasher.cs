using System.Security.Cryptography;
using System.Text;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class DirectoryContentHasher : IContentHasher
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.Ordinal)
    {
        ".git",
        "bin",
        "obj",
        "TestResults"
    };

    public async Task<string> HashDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        if (!Directory.Exists(directory))
        {
            return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
        }

        foreach (var file in EnumerateFiles(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/');
            hasher.AppendData(Encoding.UTF8.GetBytes(relativePath));
            hasher.AppendData([0]);
            hasher.AppendData(await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false));
            hasher.AppendData([0]);
        }

        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
    }

    private static IEnumerable<string> EnumerateFiles(string directory)
    {
        return Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(file => !HasExcludedDirectory(directory, file))
            .Order(StringComparer.Ordinal);
    }

    private static bool HasExcludedDirectory(string rootDirectory, string file)
    {
        var relativePath = Path.GetRelativePath(rootDirectory, file);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => ExcludedDirectoryNames.Contains(part));
    }
}
