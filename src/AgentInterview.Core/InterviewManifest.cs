using System.Text.Json.Serialization;

namespace AgentInterview.Core;

public sealed record InterviewManifest
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; init; } = string.Empty;

    [JsonPropertyName("runtime")]
    public RuntimeSpec Runtime { get; init; } = new();

    [JsonPropertyName("limits")]
    public ResourceLimits Limits { get; init; } = new();

    [JsonPropertyName("candidate")]
    public CandidateSpec Candidate { get; init; } = new();

    [JsonPropertyName("grading")]
    public GradingSpec Grading { get; init; } = new();

    [JsonPropertyName("networkAccess")]
    public bool NetworkAccess { get; init; }
}

public sealed record RuntimeSpec
{
    [JsonPropertyName("language")]
    public string Language { get; init; } = string.Empty;

    [JsonPropertyName("framework")]
    public string Framework { get; init; } = string.Empty;
}

public sealed record ResourceLimits
{
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; init; }

    [JsonPropertyName("maxInputTokens")]
    public int MaxInputTokens { get; init; }

    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; init; }

    [JsonPropertyName("maxToolCalls")]
    public int MaxToolCalls { get; init; }
}

public sealed record CandidateSpec
{
    [JsonPropertyName("instructions")]
    public string Instructions { get; init; } = string.Empty;

    [JsonPropertyName("workspace")]
    public string Workspace { get; init; } = string.Empty;
}

public sealed record GradingSpec
{
    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    [JsonPropertyName("passThreshold")]
    public int PassThreshold { get; init; }

    [JsonPropertyName("maximumScore")]
    public int MaximumScore { get; init; }
}
