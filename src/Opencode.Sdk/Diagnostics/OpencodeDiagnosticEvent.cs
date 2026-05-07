using System.Net;

namespace Opencode.Diagnostics;

/// <summary>
/// Represents a diagnostics event emitted by the shared HTTP pipeline.
/// </summary>
public sealed class OpencodeDiagnosticEvent
{
    /// <summary>
    /// Gets the event kind.
    /// </summary>
    public required OpencodeDiagnosticEventKind Kind { get; init; }

    /// <summary>
    /// Gets the HTTP method associated with the request.
    /// </summary>
    public required HttpMethod Method { get; init; }

    /// <summary>
    /// Gets the request path associated with the event.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the 1-based attempt number associated with the event.
    /// </summary>
    public required int Attempt { get; init; }

    /// <summary>
    /// Gets the effective timeout applied to the request.
    /// </summary>
    public required TimeSpan Timeout { get; init; }

    /// <summary>
    /// Gets the elapsed time for the request attempt when applicable.
    /// </summary>
    public required TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Gets the HTTP status code associated with the event when applicable.
    /// </summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>
    /// Gets the retry delay associated with the event when applicable.
    /// </summary>
    public TimeSpan? RetryDelay { get; init; }

    /// <summary>
    /// Gets the exception type associated with the event when applicable.
    /// </summary>
    public string? ExceptionType { get; init; }
}