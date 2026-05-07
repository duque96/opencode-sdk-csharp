using System.Collections.ObjectModel;
using System.Net;

namespace Opencode.Errors;

/// <summary>
/// Represents an HTTP error returned by the Opencode API.
/// </summary>
public class ApiException : OpencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException" /> class.
    /// </summary>
    public ApiException(
        HttpStatusCode statusCode,
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(message ?? CreateDefaultMessage(statusCode, reasonPhrase), innerException)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Headers = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            new Dictionary<string, IReadOnlyList<string>>(headers, StringComparer.OrdinalIgnoreCase));
        ResponseBody = responseBody;
        RequestId = requestId;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the HTTP reason phrase returned by the API when available.
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// Gets the normalized response headers associated with the failure.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

    /// <summary>
    /// Gets the raw response body associated with the failure when present.
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Gets the request identifier inferred from the response headers when available.
    /// </summary>
    public string? RequestId { get; }

    private static string CreateDefaultMessage(HttpStatusCode statusCode, string? reasonPhrase)
    {
        return string.IsNullOrWhiteSpace(reasonPhrase)
            ? $"The API returned status code {(int)statusCode}."
            : $"The API returned status code {(int)statusCode} ({reasonPhrase}).";
    }
}