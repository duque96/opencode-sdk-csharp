namespace Opencode.Errors;

/// <summary>
/// Represents the base exception type for errors raised by the Opencode SDK.
/// </summary>
public class OpencodeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpencodeException" /> class.
    /// </summary>
    public OpencodeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpencodeException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OpencodeException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpencodeException" /> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public OpencodeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}