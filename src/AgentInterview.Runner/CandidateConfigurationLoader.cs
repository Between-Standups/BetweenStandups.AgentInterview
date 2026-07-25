using System.Text.Json;
using System.Text.Json.Serialization;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public static class CandidateConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<CandidateConfiguration> LoadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Candidate configuration '{path}' was not found.");
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var document = JsonSerializer.Deserialize<CandidateConfigurationDocument>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Candidate configuration '{path}' could not be read.");

        var errors = new List<string>();
        Require(document.Provider, "provider", errors);
        Require(document.Model, "model", errors);
        Require(document.AgentConfiguration, "agentConfiguration", errors);
        Require(document.PromptVersion, "promptVersion", errors);
        Require(document.Adapter, "adapter", errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Candidate configuration '{path}' is invalid: {string.Join(" ", errors)}");
        }

        return new CandidateConfiguration(
            document.Provider,
            document.Model,
            document.AgentConfiguration,
            document.PromptVersion,
            document.Adapter);
    }

    private static void Require(string value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} is required.");
        }
    }

    private sealed record CandidateConfigurationDocument
    {
        [JsonPropertyName("provider")]
        public string Provider { get; init; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("agentConfiguration")]
        public string AgentConfiguration { get; init; } = string.Empty;

        [JsonPropertyName("promptVersion")]
        public string PromptVersion { get; init; } = string.Empty;

        [JsonPropertyName("adapter")]
        public string Adapter { get; init; } = string.Empty;
    }
}
