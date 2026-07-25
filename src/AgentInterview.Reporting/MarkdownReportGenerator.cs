using AgentInterview.Core;

namespace AgentInterview.Reporting;

public sealed class MarkdownReportGenerator : IReportGenerator
{
    public Task GenerateAsync(ReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(request.OutputDirectory);
        return Task.CompletedTask;
    }
}
