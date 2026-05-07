using Opencode.Client;
using Opencode.Internal.Http;

namespace Opencode.Resources.Tui;

/// <summary>
/// Provides access to terminal UI operations.
/// </summary>
public sealed class TuiClient
{
    private readonly OpencodeClient _client;

    internal TuiClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Appends a prompt to the terminal UI input.
    /// </summary>
    /// <param name="text">The prompt text to append.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the prompt was accepted by the terminal UI.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is empty or whitespace.</exception>
    public Task<bool> AppendPromptAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The prompt text must not be empty.", nameof(text));
        }

        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/tui/append-prompt",
            Body = new
            {
                text,
            },
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Opens the terminal UI help dialog.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the help dialog was opened.</returns>
    public Task<bool> OpenHelpAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/tui/open-help",
            CancellationToken = cancellationToken,
        });
    }
}