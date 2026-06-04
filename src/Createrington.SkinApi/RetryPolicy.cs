namespace Createrington.SkinApi;

internal static class RetryPolicy
{
    private const int BackoffBaseMs = 200;
    private const int BackoffMaxMs = 5_000;

    // Bounds how long a server-provided retryAfterMs may pause a retry, so a
    // misbehaving server cannot make the client hang. The true value is still
    // surfaced on SkinApiException.RetryAfterMs.
    internal const int RetryAfterMaxMs = 60_000;

    internal static bool IsRetryableStatus(int status) =>
        status is 429 or 502 or 503 or 504;

    internal static TimeSpan Backoff(int attempt, double jitterFraction)
    {
        // Clamp the shift so a large Retries value cannot overflow the int
        // multiplication into a negative delay; 200 * 2^5 already exceeds the
        // cap, so clamping never changes the resulting delay.
        var capped = Math.Min(BackoffBaseMs * (1 << Math.Min(attempt, 16)), BackoffMaxMs);
        var jitter = capped * 0.25 * jitterFraction;
        return TimeSpan.FromMilliseconds(capped + jitter);
    }

    internal static TimeSpan RetryDelay(int status, int? retryAfterMs, int attempt, double jitterFraction)
    {
        if (status == 429 && retryAfterMs is int ms && ms > 0)
        {
            return TimeSpan.FromMilliseconds(Math.Min(ms, RetryAfterMaxMs));
        }

        return Backoff(attempt, jitterFraction);
    }
}
