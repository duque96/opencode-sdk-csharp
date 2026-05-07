using System.Net;

namespace Opencode.Internal.Retry;

internal sealed class RetryDecider
{
    public static bool ShouldRetry(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Headers.TryGetValues("x-should-retry", out var values))
        {
            var value = values.FirstOrDefault();
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return response.StatusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.Conflict
            or (HttpStatusCode)429
            || (int)response.StatusCode >= 500;
    }
}