using System.Reflection;
using Opencode.Client;
using Opencode.Diagnostics;
using Opencode.Internal.Serialization;
using Opencode.Internal.Headers;
using Opencode.Internal.Urls;

namespace Opencode.Internal.Http;

internal sealed class HttpPipelineOptions
{
    private HttpPipelineOptions(
        Uri baseUrl,
        HttpClient httpClient,
        TimeSpan timeout,
        int maxRetries,
        IReadOnlyDictionary<string, string?> defaultHeaders,
        JsonSerializerProvider serializerProvider,
        Action<OpencodeDiagnosticEvent>? diagnosticsHandler,
        string userAgent,
        IReadOnlyDictionary<string, string> sdkHeaders)
    {
        BaseUrl = baseUrl;
        HttpClient = httpClient;
        Timeout = timeout;
        MaxRetries = maxRetries;
        DefaultHeaders = defaultHeaders;
        SerializerProvider = serializerProvider;
        DiagnosticsHandler = diagnosticsHandler;
        UserAgent = userAgent;
        SdkHeaders = sdkHeaders;
    }

    public Uri BaseUrl { get; }

    public HttpClient HttpClient { get; }

    public TimeSpan Timeout { get; }

    public int MaxRetries { get; }

    public IReadOnlyDictionary<string, string?> DefaultHeaders { get; }

    public JsonSerializerProvider SerializerProvider { get; }

    public Action<OpencodeDiagnosticEvent>? DiagnosticsHandler { get; }

    public string UserAgent { get; }

    public IReadOnlyDictionary<string, string> SdkHeaders { get; }

    public static HttpPipelineOptions Create(OpencodeClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.BaseUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("The base URL must be absolute.", nameof(options));
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The timeout must be greater than zero.");
        }

        if (options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The maximum retries must be zero or greater.");
        }

        var assemblyName = typeof(HttpPipelineOptions).Assembly.GetName();
        var version = assemblyName.Version?.ToString() ?? "0.0.0";
        var userAgent = $"Opencode.Sdk/{version}";
        var sdkHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Stainless-Lang"] = "dotnet",
            ["X-Stainless-Package-Version"] = version,
        };

        return new HttpPipelineOptions(
            options.BaseUrl,
            options.HttpClient ?? CreateDefaultHttpClient(),
            options.Timeout,
            options.MaxRetries,
            new Dictionary<string, string?>(options.DefaultHeaders, StringComparer.OrdinalIgnoreCase),
            new JsonSerializerProvider(options.JsonSerializerOptions),
            options.DiagnosticsHandler,
            userAgent,
            sdkHeaders);
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        return new HttpClient
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan,
        };
    }
}