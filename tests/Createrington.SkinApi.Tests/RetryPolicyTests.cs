namespace Createrington.SkinApi.Tests;

public sealed class RetryPolicyTests
{
    [Fact]
    public void RetryDelay_HonorsPositiveRetryAfter()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(5), RetryPolicy.RetryDelay(429, 5, 0, 0.0));
    }

    [Fact]
    public void RetryDelay_CapsExcessiveRetryAfter()
    {
        var oneDayMs = 24 * 60 * 60 * 1000;
        Assert.Equal(
            TimeSpan.FromMilliseconds(RetryPolicy.RetryAfterMaxMs),
            RetryPolicy.RetryDelay(429, oneDayMs, 0, 0.0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void RetryDelay_FallsBackToBackoffForNonPositive(int retryAfterMs)
    {
        // A negative delay would throw in Task.Delay; zero would retry instantly.
        Assert.Equal(TimeSpan.FromMilliseconds(200), RetryPolicy.RetryDelay(429, retryAfterMs, 0, 0.0));
    }

    [Fact]
    public void RetryDelay_UsesBackoffForNon429()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(200), RetryPolicy.RetryDelay(503, 5, 0, 0.0));
    }

    [Fact]
    public void Backoff_AddsJitterUpToTwentyFivePercent()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(200), RetryPolicy.Backoff(0, 0.0));
        Assert.Equal(TimeSpan.FromMilliseconds(250), RetryPolicy.Backoff(0, 1.0));
    }

    [Fact]
    public void Backoff_DoesNotOverflowAtHighAttempt()
    {
        // A large Retries value drives attempt high; the shift must not overflow
        // into a negative delay.
        Assert.Equal(TimeSpan.FromMilliseconds(5000), RetryPolicy.Backoff(40, 0.0));
    }

    [Fact]
    public void IsRetryableStatus_CoversTransientStatuses()
    {
        Assert.True(RetryPolicy.IsRetryableStatus(429));
        Assert.True(RetryPolicy.IsRetryableStatus(503));
        Assert.False(RetryPolicy.IsRetryableStatus(404));
        Assert.False(RetryPolicy.IsRetryableStatus(500));
    }
}
