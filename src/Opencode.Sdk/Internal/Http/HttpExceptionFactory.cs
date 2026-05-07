using System.Collections.ObjectModel;
using System.Net;
using Opencode.Errors;

namespace Opencode.Internal.Http;

internal static class HttpExceptionFactory
{
    public static async Task<ApiException> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        var responseBody = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            responseBody = null;
        }

        var headers = NormalizeHeaders(response);
        var requestId = TryGetRequestId(headers);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new BadRequestException(response.ReasonPhrase, headers, responseBody, requestId),
            HttpStatusCode.Unauthorized => new UnauthorizedException(response.ReasonPhrase, headers, responseBody, requestId),
            HttpStatusCode.Forbidden => new ForbiddenException(response.ReasonPhrase, headers, responseBody, requestId),
            HttpStatusCode.NotFound => new NotFoundException(response.ReasonPhrase, headers, responseBody, requestId),
            HttpStatusCode.Conflict => new ConflictException(response.ReasonPhrase, headers, responseBody, requestId),
            HttpStatusCode.UnprocessableEntity => new UnprocessableEntityException(response.ReasonPhrase, headers, responseBody, requestId),
            HttpStatusCode.TooManyRequests => new RateLimitException(response.ReasonPhrase, headers, responseBody, requestId),
            >= HttpStatusCode.InternalServerError => new InternalServerErrorException(response.StatusCode, response.ReasonPhrase, headers, responseBody, requestId),
            _ => new ApiException(response.StatusCode, response.ReasonPhrase, headers, responseBody, requestId),
        };
    }

    private static ReadOnlyDictionary<string, IReadOnlyList<string>> NormalizeHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = header.Value.ToArray();
            }
        }

        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(headers);
    }

    private static string? TryGetRequestId(IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        return TryGetFirstValue(headers, "x-request-id")
            ?? TryGetFirstValue(headers, "request-id");
    }

    private static string? TryGetFirstValue(IReadOnlyDictionary<string, IReadOnlyList<string>> headers, string key)
    {
        return headers.TryGetValue(key, out var values) && values.Count > 0 ? values[0] : null;
    }
}