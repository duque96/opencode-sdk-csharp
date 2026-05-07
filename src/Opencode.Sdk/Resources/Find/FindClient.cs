using Opencode.Client;
using Opencode.Internal.Http;
using Opencode.Models.Find;

namespace Opencode.Resources.Find;

/// <summary>
/// Provides access to search operations.
/// </summary>
public sealed class FindClient
{
    private readonly OpencodeClient _client;

    internal FindClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Finds workspace files matching the provided query.
    /// </summary>
    /// <param name="query">The file search query to execute.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The matching workspace file paths.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is empty or whitespace.</exception>
    public Task<string[]?> FindFilesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("The search query must not be empty.", nameof(query));
        }

        return _client.Pipeline.SendAsync<string[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/find/file",
            Query = new Dictionary<string, object?>
            {
                ["query"] = query,
            },
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Finds workspace symbols matching the provided query.
    /// </summary>
    /// <param name="query">The symbol search query to execute.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The matching workspace symbols.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is empty or whitespace.</exception>
    public Task<FindSymbol[]?> FindSymbolsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("The search query must not be empty.", nameof(query));
        }

        return _client.Pipeline.SendAsync<FindSymbol[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/find/symbol",
            Query = new Dictionary<string, object?>
            {
                ["query"] = query,
            },
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Finds text matches matching the provided pattern.
    /// </summary>
    /// <param name="pattern">The text pattern to search for.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The matching text occurrences.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern"/> is empty or whitespace.</exception>
    public Task<FindTextMatch[]?> FindTextAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("The search pattern must not be empty.", nameof(pattern));
        }

        return _client.Pipeline.SendAsync<FindTextMatch[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/find",
            Query = new Dictionary<string, object?>
            {
                ["pattern"] = pattern,
            },
            CancellationToken = cancellationToken,
        });
    }
}