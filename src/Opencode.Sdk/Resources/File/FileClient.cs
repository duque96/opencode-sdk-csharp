using Opencode.Client;
using Opencode.Internal.Http;
using Opencode.Models.File;

namespace Opencode.Resources.File;

/// <summary>
/// Provides access to file operations.
/// </summary>
public sealed class FileClient
{
    private readonly OpencodeClient _client;

    internal FileClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Reads the current content or patch representation for a workspace file.
    /// </summary>
    /// <param name="path">The workspace-relative path of the file to read.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The file content payload returned by the API.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    public Task<FileReadResponse?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The file path must not be empty.", nameof(path));
        }

        return _client.Pipeline.SendAsync<FileReadResponse>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/file",
            Query = new Dictionary<string, object?>
            {
                ["path"] = path,
            },
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Gets the current tracked workspace file statuses.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The file status entries returned by the API.</returns>
    public Task<FileStatusEntry[]?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<FileStatusEntry[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/file/status",
            CancellationToken = cancellationToken,
        });
    }
}