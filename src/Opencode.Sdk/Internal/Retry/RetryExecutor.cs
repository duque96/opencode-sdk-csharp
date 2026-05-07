using System.Net.Http.Headers;

namespace Opencode.Internal.Retry;

internal sealed class RetryExecutor
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(8);

    public static TimeSpan GetDelay(HttpResponseHeaders? headers, int retryCount)
    {
        if (headers is not null && TryGetServerDelay(headers, out var serverDelay))
        {
            return serverDelay;
        }

        var exponentialMilliseconds = InitialDelay.TotalMilliseconds * Math.Pow(2, retryCount);
        return TimeSpan.FromMilliseconds(Math.Min(exponentialMilliseconds, MaximumDelay.TotalMilliseconds));
    }

    public static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(delay, cancellationToken);
    }

    private static bool TryGetServerDelay(HttpResponseHeaders headers, out TimeSpan delay)
    {
        if (TryReadMilliseconds(headers, "retry-after-ms", out delay))
        {
            return true;
        }

        if (!headers.TryGetValues("retry-after", out var values))
        {
            delay = default;
            return false;
        }

        var value = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
        {
            delay = default;
            return false;
        }

        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            delay = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return true;
        }

        if (DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var retryAt))
        {
            delay = retryAt - DateTimeOffset.UtcNow;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            return true;
        }

        delay = default;
        return false;
    }

    private static bool TryReadMilliseconds(HttpResponseHeaders headers, string name, out TimeSpan delay)
    {
        if (headers.TryGetValues(name, out var values)
            && double.TryParse(values.FirstOrDefault(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var milliseconds))
        {
            delay = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return true;
        }

        delay = default;
        return false;
    }
}