namespace Opencode.Diagnostics;

/// <summary>
/// Describes the stable diagnostics events emitted by the SDK HTTP pipeline.
/// </summary>
public enum OpencodeDiagnosticEventKind
{
    /// <summary>
    /// Emitted immediately before an HTTP request attempt is sent.
    /// </summary>
    RequestStarted,

    /// <summary>
    /// Emitted after response headers are received for an HTTP request attempt.
    /// </summary>
    ResponseReceived,

    /// <summary>
    /// Emitted when the pipeline schedules a retry after a transient failure.
    /// </summary>
    RetryScheduled,

    /// <summary>
    /// Emitted when the pipeline throws a public exception to the caller.
    /// </summary>
    ExceptionThrown,
}