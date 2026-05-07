namespace Opencode.Internal.Http;

internal sealed class HttpPipelineRequest
{
    public required HttpMethod Method { get; init; }

    public required string Path { get; init; }

    public IReadOnlyDictionary<string, object?>? Query { get; init; }

    public IReadOnlyDictionary<string, string?>? Headers { get; init; }

    public object? Body { get; init; }

    public TimeSpan? Timeout { get; init; }

    public int? MaxRetries { get; init; }

    public CancellationToken CancellationToken { get; init; }
}