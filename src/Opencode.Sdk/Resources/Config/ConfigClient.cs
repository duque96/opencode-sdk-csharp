using Opencode.Client;
using Opencode.Internal.Http;
using Opencode.Models.Config;

namespace Opencode.Resources.Config;

/// <summary>
/// Provides access to configuration operations.
/// </summary>
public sealed class ConfigClient
{
    private readonly OpencodeClient _client;

    internal ConfigClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Gets the current configuration document returned by the API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The current configuration payload.</returns>
    public Task<ConfigInfo?> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<ConfigInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/config",
            CancellationToken = cancellationToken,
        });
    }
}