using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Opencode.Internal.Headers;
using Opencode.Internal.Serialization;
using Opencode.Internal.Urls;

namespace Opencode.Internal.Http;

internal sealed class HttpRequestFactory
{
    private readonly RequestUriBuilder _requestUriBuilder;
    private readonly HeaderBuilder _headerBuilder;
    private readonly JsonSerializerProvider _serializerProvider;

    public HttpRequestFactory(
        RequestUriBuilder requestUriBuilder,
        HeaderBuilder headerBuilder,
        JsonSerializerProvider serializerProvider)
    {
        _requestUriBuilder = requestUriBuilder;
        _headerBuilder = headerBuilder;
        _serializerProvider = serializerProvider;
    }

    public HttpRequestMessage Create(HttpPipelineOptions options, HttpPipelineRequest request, int retryCount = 0)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);

        var effectiveTimeout = request.Timeout ?? options.Timeout;
        var requestMessage = new HttpRequestMessage(request.Method, RequestUriBuilder.Build(options.BaseUrl, request.Path, request.Query));

        var content = CreateContent(request.Body, out var bodyHeaders);
        if (content is not null)
        {
            requestMessage.Content = content;
        }

        var headers = HeaderBuilder.Build(
            options.UserAgent,
            options.SdkHeaders,
            options.DefaultHeaders,
            bodyHeaders,
            request.Headers,
            effectiveTimeout,
            retryCount);

        foreach (var (name, value) in headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(name, value))
            {
                requestMessage.Content ??= new ByteArrayContent([]);
                requestMessage.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return requestMessage;
    }

    private HttpContent? CreateContent(object? body, out IReadOnlyDictionary<string, string?>? bodyHeaders)
    {
        bodyHeaders = null;

        if (body is null)
        {
            return null;
        }

        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        var json = JsonSerializer.Serialize(body, _serializerProvider.Options);
        bodyHeaders = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "application/json; charset=utf-8",
        };

        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}