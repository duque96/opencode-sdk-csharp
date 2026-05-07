using System.Text.Json;
using Opencode.Diagnostics;

namespace Opencode.Client;

/// <summary>
/// Configures shared settings for an <see cref="OpencodeClient" /> instance.
/// </summary>
public sealed class OpencodeClientOptions
{
    /// <summary>
    /// Gets or sets the base address for the Opencode API.
    /// </summary>
    public Uri BaseUrl { get; set; } = new("http://localhost:54321", UriKind.Absolute);

    /// <summary>
    /// Gets or sets the HTTP client used by the shared transport pipeline.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Gets the default headers that are applied to every request.
    /// </summary>
    public IDictionary<string, string?> DefaultHeaders { get; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the default timeout applied to requests.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Gets or sets the JSON serializer options used by the shared transport pipeline.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    /// <summary>
    /// Gets or sets an optional callback that receives stable diagnostics events from the shared HTTP pipeline.
    /// </summary>
    public Action<OpencodeDiagnosticEvent>? DiagnosticsHandler { get; set; }
}