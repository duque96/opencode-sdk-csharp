namespace Opencode.Internal.Urls;

internal sealed class RequestUriBuilder
{
    public static Uri Build(Uri baseUrl, string path, IReadOnlyDictionary<string, object?>? query)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var requestUri = TryCreateAbsoluteHttpUri(path, out var absoluteUri)
            ? new UriBuilder(absoluteUri)
            : new UriBuilder(baseUrl)
            {
                Path = CombinePath(baseUrl.AbsolutePath, path),
            };

        var queryString = QueryStringBuilder.Build(query);
        if (!string.IsNullOrEmpty(queryString))
        {
            requestUri.Query = AppendQuery(requestUri.Query, queryString);
        }

        return requestUri.Uri;
    }

    private static string CombinePath(string basePath, string relativePath)
    {
        var normalizedBase = string.IsNullOrEmpty(basePath) ? "/" : basePath;
        var trimmedBase = normalizedBase.TrimEnd('/');
        var trimmedRelative = relativePath.TrimStart('/');

        return string.IsNullOrEmpty(trimmedRelative)
            ? (string.IsNullOrEmpty(trimmedBase) ? "/" : trimmedBase)
            : $"{trimmedBase}/{trimmedRelative}";
    }

    private static string AppendQuery(string existingQuery, string newQuery)
    {
        var trimmedExisting = existingQuery.TrimStart('?');
        if (string.IsNullOrEmpty(trimmedExisting))
        {
            return newQuery;
        }

        return $"{trimmedExisting}&{newQuery}";
    }

    private static bool TryCreateAbsoluteHttpUri(string path, out Uri absoluteUri)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out absoluteUri!)
            && (absoluteUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || absoluteUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        absoluteUri = null!;
        return false;
    }
}