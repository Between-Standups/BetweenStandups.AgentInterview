using AgentInterview.Core;

namespace AgentInterview.Core.Tests;

public sealed class ManifestSchemaValidatorTests
{
    [Fact]
    public void ValidateJsonAcceptsSeedShape()
    {
        var result = ManifestSchemaValidator.ValidateJson(ValidManifestJson());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateJsonRejectsMissingRequiredProperties()
    {
        var result = ManifestSchemaValidator.ValidateJson(
            """
            {
              "schemaVersion": "1.0"
            }
            """);

        Assert.Contains("id is required.", result.Errors);
        Assert.Contains("runtime is required.", result.Errors);
        Assert.Contains("networkAccess is required.", result.Errors);
    }

    [Fact]
    public void ValidateJsonRejectsUnknownProperties()
    {
        var result = ManifestSchemaValidator.ValidateJson(
            """
            {
              "schemaVersion": "1.0",
              "id": "coding.calculator-api",
              "version": "1.0.0",
              "title": "Implement a Calculator API",
              "category": "coding",
              "difficulty": "mid",
              "runtime": {
                "language": "csharp",
                "framework": "net10.0"
              },
              "limits": {
                "timeoutSeconds": 900,
                "maxInputTokens": 25000,
                "maxOutputTokens": 15000,
                "maxToolCalls": 100
              },
              "candidate": {
                "instructions": "prompt.md",
                "workspace": "starter"
              },
              "grading": {
                "command": "dotnet test grader/AgentInterview.Grader.csproj",
                "passThreshold": 100,
                "maximumScore": 100
              },
              "networkAccess": false,
              "extra": true
            }
            """);

        Assert.Contains("extra is not allowed.", result.Errors);
    }

    [Fact]
    public void ValidateJsonRejectsInvalidTypesAndLimits()
    {
        var result = ManifestSchemaValidator.ValidateJson(
            """
            {
              "schemaVersion": "1.0",
              "id": "coding.calculator-api",
              "version": "1.0.0",
              "title": "Implement a Calculator API",
              "category": "coding",
              "difficulty": "mid",
              "runtime": {
                "language": "csharp",
                "framework": "net10.0"
              },
              "limits": {
                "timeoutSeconds": 0,
                "maxInputTokens": "lots",
                "maxOutputTokens": 15000,
                "maxToolCalls": 100
              },
              "candidate": {
                "instructions": "prompt.md",
                "workspace": "starter"
              },
              "grading": {
                "command": "dotnet test grader/AgentInterview.Grader.csproj",
                "passThreshold": -1,
                "maximumScore": 100
              },
              "networkAccess": true
            }
            """);

        Assert.Contains("limits.timeoutSeconds must be greater than zero.", result.Errors);
        Assert.Contains("limits.maxInputTokens must be an integer.", result.Errors);
        Assert.Contains("grading.passThreshold must be zero or greater.", result.Errors);
        Assert.Contains("networkAccess must be false.", result.Errors);
    }

    [Fact]
    public void ValidateJsonRejectsMalformedJson()
    {
        var result = ManifestSchemaValidator.ValidateJson("{");

        Assert.Contains("manifest must contain valid JSON.", result.Errors);
    }

    private static string ValidManifestJson() =>
        """
        {
          "schemaVersion": "1.0",
          "id": "coding.calculator-api",
          "version": "1.0.0",
          "title": "Implement a Calculator API",
          "category": "coding",
          "difficulty": "mid",
          "runtime": {
            "language": "csharp",
            "framework": "net10.0"
          },
          "limits": {
            "timeoutSeconds": 900,
            "maxInputTokens": 25000,
            "maxOutputTokens": 15000,
            "maxToolCalls": 100
          },
          "candidate": {
            "instructions": "prompt.md",
            "workspace": "starter"
          },
          "grading": {
            "command": "dotnet test grader/AgentInterview.Grader.csproj",
            "passThreshold": 100,
            "maximumScore": 100
          },
          "networkAccess": false
        }
        """;
}
