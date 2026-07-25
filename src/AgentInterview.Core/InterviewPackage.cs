namespace AgentInterview.Core;

public sealed record InterviewPackage(
    InterviewManifest Manifest,
    string ManifestPath,
    string PackageDirectory);

public sealed record InterviewSummary(
    string Id,
    string Version,
    string Title,
    string Category,
    string Difficulty,
    string ManifestPath);
