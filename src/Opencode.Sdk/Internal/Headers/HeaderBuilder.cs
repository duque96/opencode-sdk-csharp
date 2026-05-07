namespace Opencode.Internal.Headers;

internal sealed class HeaderBuilder
{
    public static IReadOnlyDictionary<string, string> Build(
        string userAgent,
        IReadOnlyDictionary<string, string> sdkHeaders,
        IReadOnlyDictionary<string, string?> defaultHeaders,
        IReadOnlyDictionary<string, string?>? bodyHeaders,
        IReadOnlyDictionary<string, string?>? requestHeaders,
        TimeSpan timeout,
        int retryCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);
        ArgumentNullException.ThrowIfNull(sdkHeaders);
        ArgumentNullException.ThrowIfNull(defaultHeaders);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Apply(headers, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Accept"] = "application/json",
            ["User-Agent"] = userAgent,
            ["X-Stainless-Retry-Count"] = retryCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["X-Stainless-Timeout"] = Math.Truncate(timeout.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
        });

        Apply(headers, sdkHeaders.ToDictionary(static entry => entry.Key, static entry => (string?)entry.Value, StringComparer.OrdinalIgnoreCase));
        Apply(headers, defaultHeaders);
        Apply(headers, bodyHeaders);
        Apply(headers, requestHeaders);

        return headers;
    }

    private static void Apply(Dictionary<string, string> target, IReadOnlyDictionary<string, string?>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var (key, value) in source)
        {
            if (value is null)
            {
                target.Remove(key);
                continue;
            }

            target[key] = value;
        }
    }
}