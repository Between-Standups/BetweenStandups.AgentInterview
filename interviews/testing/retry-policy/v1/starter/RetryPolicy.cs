namespace RetryPolicyInterview;

public sealed class RetryPolicy
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries,
        Func<int, TimeSpan> delayForAttempt,
        Func<Exception, bool> isRetryable,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception exception) when (attempt < maxRetries && isRetryable(exception))
            {
                await Task.Delay(delayForAttempt(attempt + 1), cancellationToken);
            }
        }
    }
}
