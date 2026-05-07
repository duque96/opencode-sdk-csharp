namespace Opencode.Models.App;

/// <summary>
/// Describes a log entry submitted to the application log endpoint.
/// </summary>
public sealed class AppLogRequest
{
    /// <summary>
    /// Gets the log level to write.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Gets the log message content.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the service name associated with the log entry.
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// Gets optional structured metadata to include with the log entry.
    /// </summary>
    public Dictionary<string, object?>? Extra { get; init; }
}