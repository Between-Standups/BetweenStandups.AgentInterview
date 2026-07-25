using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentInterview.Core;

public static partial class ManifestSchemaValidator
{
    private static readonly string[] RootProperties =
    [
        "schemaVersion",
        "id",
        "version",
        "title",
        "category",
        "difficulty",
        "runtime",
        "limits",
        "candidate",
        "grading",
        "networkAccess"
    ];

    private static readonly string[] RuntimeProperties = ["language", "framework"];
    private static readonly string[] LimitsProperties = ["timeoutSeconds", "maxInputTokens", "maxOutputTokens", "maxToolCalls"];
    private static readonly string[] CandidateProperties = ["instructions", "workspace"];
    private static readonly string[] GradingProperties = ["command", "passThreshold", "maximumScore"];

    public static ValidationResult ValidateJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var parsedDocument = Parse(json);
        if (parsedDocument is null)
        {
            return new ValidationResult(["manifest must contain valid JSON."]);
        }

        using var document = parsedDocument;
        var errors = new List<string>();
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new ValidationResult(["manifest root must be an object."]);
        }

        var root = document.RootElement;
        ValidateObject(root, string.Empty, RootProperties, RootProperties, errors);
        ValidateString(root, "schemaVersion", "1.0", errors);
        ValidateString(root, "id", requiredValue: null, errors);
        ValidateString(root, "version", requiredValue: null, errors);
        ValidateString(root, "title", requiredValue: null, errors);
        ValidateString(root, "category", requiredValue: null, errors);
        ValidateString(root, "difficulty", requiredValue: null, errors);
        ValidateBoolean(root, "networkAccess", requiredValue: false, errors);

        if (TryGetObject(root, "runtime", errors, out var runtime))
        {
            ValidateObject(runtime, "runtime", RuntimeProperties, RuntimeProperties, errors);
            ValidateString(runtime, "language", requiredValue: null, errors, "runtime.language");
            ValidateString(runtime, "framework", requiredValue: null, errors, "runtime.framework");
        }

        if (TryGetObject(root, "limits", errors, out var limits))
        {
            ValidateObject(limits, "limits", LimitsProperties, LimitsProperties, errors);
            ValidatePositiveInteger(limits, "timeoutSeconds", errors, "limits.timeoutSeconds");
            ValidatePositiveInteger(limits, "maxInputTokens", errors, "limits.maxInputTokens");
            ValidatePositiveInteger(limits, "maxOutputTokens", errors, "limits.maxOutputTokens");
            ValidatePositiveInteger(limits, "maxToolCalls", errors, "limits.maxToolCalls");
        }

        if (TryGetObject(root, "candidate", errors, out var candidate))
        {
            ValidateObject(candidate, "candidate", CandidateProperties, CandidateProperties, errors);
            ValidateString(candidate, "instructions", requiredValue: null, errors, "candidate.instructions");
            ValidateString(candidate, "workspace", requiredValue: null, errors, "candidate.workspace");
        }

        if (TryGetObject(root, "grading", errors, out var grading))
        {
            ValidateObject(grading, "grading", GradingProperties, GradingProperties, errors);
            ValidateString(grading, "command", requiredValue: null, errors, "grading.command");
            ValidateNonNegativeInteger(grading, "passThreshold", errors, "grading.passThreshold");
            ValidatePositiveInteger(grading, "maximumScore", errors, "grading.maximumScore");
        }

        if (root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
        {
            var idValue = id.GetString();
            if (string.IsNullOrWhiteSpace(idValue) || !ManifestIdRegex().IsMatch(idValue))
            {
                errors.Add("id must match pattern 'category.interview-id'.");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(errors);
    }

    private static JsonDocument? Parse(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ValidateObject(
        JsonElement element,
        string path,
        IReadOnlyCollection<string> requiredProperties,
        IReadOnlyCollection<string> allowedProperties,
        List<string> errors)
    {
        foreach (var requiredProperty in requiredProperties)
        {
            if (!element.TryGetProperty(requiredProperty, out _))
            {
                errors.Add($"{BuildPath(path, requiredProperty)} is required.");
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name, StringComparer.Ordinal))
            {
                errors.Add($"{BuildPath(path, property.Name)} is not allowed.");
            }
        }
    }

    private static bool TryGetObject(JsonElement element, string propertyName, List<string> errors, out JsonElement value)
    {
        if (!element.TryGetProperty(propertyName, out value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        errors.Add($"{propertyName} must be an object.");
        return false;
    }

    private static void ValidateString(
        JsonElement element,
        string propertyName,
        string? requiredValue,
        List<string> errors,
        string? displayName = null)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return;
        }

        var path = displayName ?? propertyName;
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{path} must be a string.");
            return;
        }

        var stringValue = value.GetString();
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            errors.Add($"{path} must not be empty.");
            return;
        }

        if (requiredValue is not null && !string.Equals(stringValue, requiredValue, StringComparison.Ordinal))
        {
            errors.Add($"{path} must be '{requiredValue}'.");
        }
    }

    private static void ValidateBoolean(JsonElement element, string propertyName, bool requiredValue, List<string> errors)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return;
        }

        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            errors.Add($"{propertyName} must be a boolean.");
            return;
        }

        if (value.GetBoolean() != requiredValue)
        {
            errors.Add($"{propertyName} must be {requiredValue.ToString().ToLowerInvariant()}.");
        }
    }

    private static void ValidatePositiveInteger(JsonElement element, string propertyName, List<string> errors, string displayName)
    {
        if (!TryGetInteger(element, propertyName, errors, displayName, out var value))
        {
            return;
        }

        if (value < 1)
        {
            errors.Add($"{displayName} must be greater than zero.");
        }
    }

    private static void ValidateNonNegativeInteger(JsonElement element, string propertyName, List<string> errors, string displayName)
    {
        if (!TryGetInteger(element, propertyName, errors, displayName, out var value))
        {
            return;
        }

        if (value < 0)
        {
            errors.Add($"{displayName} must be zero or greater.");
        }
    }

    private static bool TryGetInteger(JsonElement element, string propertyName, List<string> errors, string displayName, out int value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out value))
        {
            errors.Add($"{displayName} must be an integer.");
            return false;
        }

        return true;
    }

    private static string BuildPath(string parent, string propertyName) =>
        string.IsNullOrEmpty(parent) ? propertyName : $"{parent}.{propertyName}";

    [GeneratedRegex("^[a-z0-9]+(\\.[a-z0-9-]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex ManifestIdRegex();
}
