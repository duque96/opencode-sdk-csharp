namespace Opencode.Errors;

/// <summary>
/// Represents an SDK-managed request timeout after retries are exhausted.
/// </summary>
public sealed class RequestTimeoutException : ConnectionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutException" /> class.
    /// </summary>
    public RequestTimeoutException(TimeSpan timeout, Exception? innerException = null)
        : base($"The HTTP request timed out after {timeout}.", innerException)
    {
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the effective timeout that elapsed before the request was aborted.
    /// </summary>
    public TimeSpan Timeout { get; }
}