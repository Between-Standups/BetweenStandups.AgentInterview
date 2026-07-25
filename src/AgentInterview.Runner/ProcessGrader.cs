using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class ProcessGrader : IGrader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<GraderRunResult> GradeAsync(GraderRunRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tokens = CommandLine.Split(request.Package.Manifest.Grading.Command);
        if (tokens.Count == 0)
        {
            return Failure("grading.command is empty.", string.Empty, request.Package.Manifest.Grading.MaximumScore);
        }

        using var process = new Process
        {
            StartInfo = CreateStartInfo(tokens, request)
        };

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.Package.Manifest.Limits.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);
            var timedOutOutput = await stdoutTask.ConfigureAwait(false);
            var timedOutError = await stderrTask.ConfigureAwait(false);
            return new GraderRunResult(
                Passed: false,
                Score: 0,
                MaximumScore: request.Package.Manifest.Grading.MaximumScore,
                Cases:
                [
                    new GraderCaseResult(
                        "grader.timeout",
                        false,
                        0,
                        $"Grader timed out after {request.Package.Manifest.Limits.TimeoutSeconds} seconds.")
                ],
                StandardOutput: timedOutOutput,
                StandardError: timedOutError);
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            return new GraderRunResult(
                Passed: false,
                Score: 0,
                MaximumScore: request.Package.Manifest.Grading.MaximumScore,
                Cases:
                [
                    new GraderCaseResult("grader.process", false, 0, $"Grader exited with code {process.ExitCode}.")
                ],
                StandardOutput: stdout,
                StandardError: stderr);
        }

        return ParseOutput(stdout, stderr, request.Package.Manifest.Grading.MaximumScore);
    }

    private static ProcessStartInfo CreateStartInfo(IReadOnlyList<string> tokens, GraderRunRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = tokens[0],
            WorkingDirectory = request.Package.PackageDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.Environment["AGENT_INTERVIEW_WORKSPACE"] = request.Workspace.CandidateWorkspaceDirectory;
        startInfo.Environment["AGENT_INTERVIEW_PACKAGE"] = request.Package.PackageDirectory;

        foreach (var argument in tokens.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static GraderRunResult ParseOutput(string stdout, string stderr, int fallbackMaximumScore)
    {
        try
        {
            var document = JsonSerializer.Deserialize<GraderOutputDocument>(stdout, SerializerOptions);
            if (document is null)
            {
                return Failure("Grader did not emit JSON output.", stdout, fallbackMaximumScore, stderr);
            }

            var cases = document.Cases
                .Select(item => new GraderCaseResult(item.Name, item.Passed, item.Score, item.Message))
                .ToArray();

            return new GraderRunResult(
                document.Passed,
                document.Score,
                document.MaximumScore,
                cases,
                stdout,
                stderr);
        }
        catch (JsonException)
        {
            return Failure("Grader emitted malformed JSON output.", stdout, fallbackMaximumScore, stderr);
        }
    }

    private static GraderRunResult Failure(string message, string stdout, int maximumScore, string standardError = "") =>
        new(
            Passed: false,
            Score: 0,
            MaximumScore: maximumScore,
            Cases: [new GraderCaseResult("grader.output", false, 0, message)],
            StandardOutput: stdout,
            StandardError: standardError);

    private static void KillProcess(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed record GraderOutputDocument
    {
        [JsonPropertyName("passed")]
        public bool Passed { get; init; }

        [JsonPropertyName("score")]
        public int Score { get; init; }

        [JsonPropertyName("maximumScore")]
        public int MaximumScore { get; init; }

        [JsonPropertyName("cases")]
        public IReadOnlyList<GraderCaseDocument> Cases { get; init; } = Array.Empty<GraderCaseDocument>();
    }

    private sealed record GraderCaseDocument
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("passed")]
        public bool Passed { get; init; }

        [JsonPropertyName("score")]
        public int Score { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }
}
