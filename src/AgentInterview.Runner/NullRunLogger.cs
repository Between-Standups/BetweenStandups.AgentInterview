using AgentInterview.Core;

namespace AgentInterview.Runner;

public sealed class NullRunLogger : IRunLogger
{
    public Task LogAsync(
        string level,
        string eventName,
        IReadOnlyDictionary<string, string> properties,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
