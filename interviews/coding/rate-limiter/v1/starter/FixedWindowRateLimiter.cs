namespace RateLimiterInterview;

public sealed class FixedWindowRateLimiter
{
    public FixedWindowRateLimiter(int limit, TimeSpan window)
    {
    }

    public bool Allow(string key, DateTimeOffset now)
    {
        throw new NotImplementedException();
    }
}
