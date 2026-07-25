using System.Globalization;
using System.Text;
using System.Text.Json;
using AgentInterview.Core;

namespace AgentInterview.Reporting;

public sealed class MarkdownReportGenerator : IReportGenerator
{
    public async Task GenerateAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(request.OutputDirectory);

        var rows = new List<ResultRow>();
        foreach (var resultPath in Directory.EnumerateFiles(request.ResultsDirectory, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(await ReadResultAsync(resultPath, cancellationToken).ConfigureAwait(false));
        }

        var groups = rows
            .GroupBy(row => new
            {
                row.Interview,
                row.Candidate
            })
            .OrderBy(group => group.Key.Interview, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Candidate, StringComparer.Ordinal)
            .Select(group => ComparisonRow.FromRows(group.Key.Interview, group.Key.Candidate, group.ToArray()))
            .ToArray();

        await File.WriteAllTextAsync(
            Path.Combine(request.OutputDirectory, "summary.csv"),
            BuildCsv(groups),
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(request.OutputDirectory, "comparison.md"),
            BuildMarkdown(groups),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ResultRow> ReadResultAsync(string resultPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(resultPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        var interview = root.GetProperty("interview");
        var candidate = root.GetProperty("candidate");
        var outcome = root.GetProperty("outcome");
        var usage = root.GetProperty("usage");
        var execution = root.GetProperty("execution");

        return new ResultRow(
            $"{interview.GetProperty("id").GetString()}@{interview.GetProperty("version").GetString()}",
            $"{candidate.GetProperty("provider").GetString()}/{candidate.GetProperty("model").GetString()}/{candidate.GetProperty("agentConfiguration").GetString()}",
            outcome.GetProperty("status").GetString() ?? string.Empty,
            outcome.GetProperty("score").GetInt32(),
            outcome.GetProperty("maximumScore").GetInt32(),
            usage.GetProperty("inputTokens").GetInt64(),
            usage.GetProperty("outputTokens").GetInt64(),
            usage.GetProperty("cachedInputTokens").GetInt64(),
            usage.GetProperty("estimatedCostUsd").GetDecimal(),
            execution.GetProperty("latencyMs").GetInt64(),
            execution.GetProperty("retries").GetInt32(),
            execution.GetProperty("toolCalls").GetInt32(),
            resultPath);
    }

    private static string BuildCsv(IReadOnlyList<ComparisonRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("interview,candidate,runs,passes,passRate,averageScore,totalTokens,totalCostUsd,costPerPassingInterview,averageLatencyMs,retries,toolCalls,determinism");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(
                ',',
                Escape(row.Interview),
                Escape(row.Candidate),
                row.Runs.ToString(CultureInfo.InvariantCulture),
                row.Passes.ToString(CultureInfo.InvariantCulture),
                row.PassRate.ToString("0.####", CultureInfo.InvariantCulture),
                row.AverageScore.ToString("0.####", CultureInfo.InvariantCulture),
                row.TotalTokens.ToString(CultureInfo.InvariantCulture),
                row.TotalCostUsd.ToString("0.####", CultureInfo.InvariantCulture),
                row.CostPerPassingInterview?.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty,
                row.AverageLatencyMs.ToString("0.####", CultureInfo.InvariantCulture),
                row.Retries.ToString(CultureInfo.InvariantCulture),
                row.ToolCalls.ToString(CultureInfo.InvariantCulture),
                Escape(row.Determinism)));
        }

        return builder.ToString();
    }

    private static string BuildMarkdown(IReadOnlyList<ComparisonRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AgentInterview Comparison");
        builder.AppendLine();
        builder.AppendLine("| Interview | Candidate | Runs | Pass Rate | Avg Score | Total Tokens | Cost / Pass | Avg Latency ms | Retries | Tool Calls | Determinism |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");

        foreach (var row in rows)
        {
            builder.Append("| ");
            builder.Append(row.Interview);
            builder.Append(" | ");
            builder.Append(row.Candidate);
            builder.Append(" | ");
            builder.Append(row.Runs.ToString(CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.PassRate.ToString("P1", CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.AverageScore.ToString("0.##", CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.TotalTokens.ToString(CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.CostPerPassingInterview?.ToString("0.####", CultureInfo.InvariantCulture) ?? "n/a");
            builder.Append(" | ");
            builder.Append(row.AverageLatencyMs.ToString("0.##", CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.Retries.ToString(CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.ToolCalls.ToString(CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.Determinism);
            builder.AppendLine(" |");
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',', StringComparison.Ordinal) && !value.Contains('"', StringComparison.Ordinal) && !value.Contains('\n', StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record ResultRow(
        string Interview,
        string Candidate,
        string Status,
        int Score,
        int MaximumScore,
        long InputTokens,
        long OutputTokens,
        long CachedInputTokens,
        decimal CostUsd,
        long LatencyMs,
        int Retries,
        int ToolCalls,
        string Path);

    private sealed record ComparisonRow(
        string Interview,
        string Candidate,
        int Runs,
        int Passes,
        decimal PassRate,
        decimal AverageScore,
        long TotalTokens,
        decimal TotalCostUsd,
        decimal? CostPerPassingInterview,
        decimal AverageLatencyMs,
        int Retries,
        int ToolCalls,
        string Determinism)
    {
        public static ComparisonRow FromRows(string interview, string candidate, IReadOnlyList<ResultRow> rows)
        {
            var passes = rows.Count(row => string.Equals(row.Status, "passed", StringComparison.Ordinal));
            return new ComparisonRow(
                interview,
                candidate,
                rows.Count,
                passes,
                rows.Count == 0 ? 0 : (decimal)passes / rows.Count,
                rows.Count == 0 ? 0 : rows.Sum(row => row.Score) / (decimal)rows.Count,
                rows.Sum(row => row.InputTokens + row.OutputTokens + row.CachedInputTokens),
                rows.Sum(row => row.CostUsd),
                passes == 0 ? null : rows.Sum(row => row.CostUsd) / passes,
                rows.Count == 0 ? 0 : rows.Sum(row => row.LatencyMs) / (decimal)rows.Count,
                rows.Sum(row => row.Retries),
                rows.Sum(row => row.ToolCalls),
                rows.Select(row => $"{row.Status}:{row.Score}/{row.MaximumScore}").Distinct(StringComparer.Ordinal).Count() == 1 ? "stable" : "variable");
        }
    }
}
