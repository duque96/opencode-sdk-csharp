using Opencode.Client;
using Opencode.Internal.Http;
using Opencode.Models.App;

namespace Opencode.Resources.App;

/// <summary>
/// Provides access to application-level operations.
/// </summary>
public sealed class AppClient
{
    private static readonly HashSet<string> AllowedLogLevels = new(StringComparer.Ordinal)
    {
        "debug",
        "info",
        "warn",
        "error",
    };

    private readonly OpencodeClient _client;

    internal AppClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Gets information about the current app environment.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The current app information.</returns>
    public Task<AppInfo?> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<AppInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/app",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Initializes the app state on the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when initialization completed successfully.</returns>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/app/init",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Writes a log entry to the server logs.
    /// </summary>
    /// <param name="request">The log entry payload to submit.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the log entry was accepted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the request contains missing or invalid values.</exception>
    public Task<bool> LogAsync(AppLogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Level))
        {
            throw new ArgumentException("The log level must not be empty.", nameof(request));
        }

        if (!AllowedLogLevels.Contains(request.Level))
        {
            throw new ArgumentException("The log level must be one of: debug, info, warn, error.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("The log message must not be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Service))
        {
            throw new ArgumentException("The service name must not be empty.", nameof(request));
        }

        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/log",
            Body = request,
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Gets the available app modes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The configured modes returned by the API.</returns>
    public Task<AppModeInfo[]?> GetModesAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<AppModeInfo[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/mode",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Gets the available model providers and provider defaults.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The provider metadata returned by the API.</returns>
    public Task<AppProvidersInfo?> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<AppProvidersInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/config/providers",
            CancellationToken = cancellationToken,
        });
    }
}