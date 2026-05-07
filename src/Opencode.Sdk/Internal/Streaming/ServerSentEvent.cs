namespace Opencode.Internal.Streaming;

internal sealed class ServerSentEvent
{
    public required string? EventName { get; init; }

    public required string Data { get; init; }

    public required IReadOnlyList<string> RawLines { get; init; }
}