namespace Opencode.Errors;

/// <summary>
/// Represents a successful HTTP response whose content type or payload did not match the SDK contract.
/// </summary>
public sealed class UnexpectedResponseException : OpencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedResponseException" /> class.
    /// </summary>
    /// <param name="message">The message that describes the response mismatch.</param>
    /// <param name="contentType">The response content type when available.</param>
    /// <param name="responseBody">The raw response body when available.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public UnexpectedResponseException(
        string message,
        string? contentType = null,
        string? responseBody = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ContentType = contentType;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the response content type when available.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the raw response body when available.
    /// </summary>
    public string? ResponseBody { get; }
}