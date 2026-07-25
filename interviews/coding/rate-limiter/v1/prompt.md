# Implement a Fixed Window Rate Limiter

Implement `FixedWindowRateLimiter.Allow(string key, DateTimeOffset now)`.

The limiter must be deterministic, per-key, thread-safe, use an injected clock value, reset counts at the window boundary, and avoid unbounded memory growth for expired windows.
