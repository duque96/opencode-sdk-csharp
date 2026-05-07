namespace Opencode.Errors;

/// <summary>
/// Represents an exhausted transport failure while sending an API request.
/// </summary>
public class ConnectionException : OpencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException" /> class.
    /// </summary>
    public ConnectionException(string? message = null, Exception? innerException = null)
        : base(message ?? "A connection error occurred while sending the request.", innerException)
    {
    }
}