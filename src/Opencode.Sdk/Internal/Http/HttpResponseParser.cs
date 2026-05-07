using System.Text.Json;
using Opencode.Errors;
using Opencode.Internal.Serialization;

namespace Opencode.Internal.Http;

internal sealed class HttpResponseParser
{
    private readonly JsonSerializerProvider _serializerProvider;

    public HttpResponseParser(JsonSerializerProvider serializerProvider)
    {
        _serializerProvider = serializerProvider;
    }

    public async ValueTask<T?> ParseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content is null)
        {
            return default;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        if (typeof(T) == typeof(string))
        {
            return (T?)(object)payload;
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!IsJsonMediaType(mediaType))
        {
            throw new UnexpectedResponseException(
                $"The server returned content type '{mediaType ?? "<unknown>"}' but the SDK expected JSON.",
                mediaType,
                payload);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, _serializerProvider.Options)
                ?? throw new UnexpectedResponseException(
                    "The server returned an empty JSON payload.",
                    mediaType,
                    payload);
        }
        catch (JsonException exception)
        {
            throw new UnexpectedResponseException(
                "The server returned a JSON payload that did not match the SDK contract.",
                mediaType,
                payload,
                exception);
        }
    }

    private static bool IsJsonMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return false;
        }

        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }
}