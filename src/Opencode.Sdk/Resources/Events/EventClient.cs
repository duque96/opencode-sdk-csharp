using Opencode.Client;
using Opencode.Errors;
using Opencode.Internal.Http;
using Opencode.Models.Events;

namespace Opencode.Resources.Events;

/// <summary>
/// Provides access to event operations.
/// </summary>
public sealed class EventClient
{
    private readonly OpencodeClient _client;

    internal EventClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Streams server events from the current Opencode instance.
    /// </summary>
    /// <param name="cancellationToken">Cancels establishing the stream or stops enumeration while events are being read.</param>
    /// <returns>An asynchronous sequence of typed event items. Enumeration ends when the server closes the stream, the consumer stops iterating, or cancellation is requested.</returns>
    /// <exception cref="OpencodeException">Thrown when the server rejects the request or when the transport fails before the stream is established.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled while connecting to or reading from the stream.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when an event payload cannot be deserialized into the public event model hierarchy.</exception>
    public IAsyncEnumerable<EventStreamItem> ListAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.StreamAsync<EventStreamItem>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/event",
            CancellationToken = cancellationToken,
        });
    }
}