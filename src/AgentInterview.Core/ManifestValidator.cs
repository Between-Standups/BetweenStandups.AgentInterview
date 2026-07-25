namespace AgentInterview.Core;

public static class ManifestValidator
{
    private const string SupportedSchemaVersion = "1.0";

    public static ValidationResult Validate(InterviewManifest manifest, string packageDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);

        var errors = new List<string>();

        Require(manifest.SchemaVersion, "schemaVersion", errors);
        Require(manifest.Id, "id", errors);
        Require(manifest.Version, "version", errors);
        Require(manifest.Title, "title", errors);
        Require(manifest.Category, "category", errors);
        Require(manifest.Difficulty, "difficulty", errors);
        Require(manifest.Runtime.Language, "runtime.language", errors);
        Require(manifest.Runtime.Framework, "runtime.framework", errors);
        Require(manifest.Candidate.Instructions, "candidate.instructions", errors);
        Require(manifest.Candidate.Workspace, "candidate.workspace", errors);
        Require(manifest.Grading.Command, "grading.command", errors);

        if (!string.Equals(manifest.SchemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
        {
            errors.Add($"schemaVersion must be '{SupportedSchemaVersion}'.");
        }

        if (manifest.Limits.TimeoutSeconds <= 0)
        {
            errors.Add("limits.timeoutSeconds must be greater than zero.");
        }

        if (manifest.Limits.MaxInputTokens <= 0)
        {
            errors.Add("limits.maxInputTokens must be greater than zero.");
        }

        if (manifest.Limits.MaxOutputTokens <= 0)
        {
            errors.Add("limits.maxOutputTokens must be greater than zero.");
        }

        if (manifest.Limits.MaxToolCalls <= 0)
        {
            errors.Add("limits.maxToolCalls must be greater than zero.");
        }

        if (manifest.Grading.MaximumScore <= 0)
        {
            errors.Add("grading.maximumScore must be greater than zero.");
        }

        if (manifest.Grading.PassThreshold < 0 || manifest.Grading.PassThreshold > manifest.Grading.MaximumScore)
        {
            errors.Add("grading.passThreshold must be between zero and grading.maximumScore.");
        }

        if (manifest.NetworkAccess)
        {
            errors.Add("networkAccess must be false for deterministic interviews.");
        }

        ValidateRelativeFile(packageDirectory, manifest.Candidate.Instructions, "candidate.instructions", errors);
        ValidateRelativeDirectory(packageDirectory, manifest.Candidate.Workspace, "candidate.workspace", errors);

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(errors);
    }

    private static void Require(string value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} is required.");
        }
    }

    private static void ValidateRelativeFile(string packageDirectory, string relativePath, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        if (Path.IsPathRooted(relativePath))
        {
            errors.Add($"{propertyName} must be a relative path.");
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(packageDirectory, relativePath));
        if (!IsInsidePackage(packageDirectory, fullPath))
        {
            errors.Add($"{propertyName} must stay inside the interview package.");
            return;
        }

        if (!File.Exists(fullPath))
        {
            errors.Add($"{propertyName} points to a missing file: {relativePath}.");
        }
    }

    private static void ValidateRelativeDirectory(string packageDirectory, string relativePath, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        if (Path.IsPathRooted(relativePath))
        {
            errors.Add($"{propertyName} must be a relative path.");
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(packageDirectory, relativePath));
        if (!IsInsidePackage(packageDirectory, fullPath))
        {
            errors.Add($"{propertyName} must stay inside the interview package.");
            return;
        }

        if (!Directory.Exists(fullPath))
        {
            errors.Add($"{propertyName} points to a missing directory: {relativePath}.");
        }
    }

    private static bool IsInsidePackage(string packageDirectory, string fullPath)
    {
        var packageRoot = Path.GetFullPath(packageDirectory);
        var relativePath = Path.GetRelativePath(packageRoot, fullPath);
        return relativePath != "."
            && !relativePath.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relativePath);
    }
}
