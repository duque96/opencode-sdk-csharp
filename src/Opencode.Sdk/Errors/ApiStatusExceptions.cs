using System.Net;

namespace Opencode.Errors;

/// <summary>
/// Represents an HTTP 400 response.
/// </summary>
public sealed class BadRequestException : ApiException
{
    internal BadRequestException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.BadRequest, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 401 response.
/// </summary>
public sealed class UnauthorizedException : ApiException
{
    internal UnauthorizedException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.Unauthorized, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 403 response.
/// </summary>
public sealed class ForbiddenException : ApiException
{
    internal ForbiddenException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.Forbidden, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 404 response.
/// </summary>
public sealed class NotFoundException : ApiException
{
    internal NotFoundException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.NotFound, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 409 response.
/// </summary>
public sealed class ConflictException : ApiException
{
    internal ConflictException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.Conflict, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 422 response.
/// </summary>
public sealed class UnprocessableEntityException : ApiException
{
    internal UnprocessableEntityException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.UnprocessableEntity, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 429 response.
/// </summary>
public sealed class RateLimitException : ApiException
{
    internal RateLimitException(
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(HttpStatusCode.TooManyRequests, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}

/// <summary>
/// Represents an HTTP 5xx response.
/// </summary>
public sealed class InternalServerErrorException : ApiException
{
    internal InternalServerErrorException(
        HttpStatusCode statusCode,
        string? reasonPhrase,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string? responseBody,
        string? requestId,
        string? message = null,
        Exception? innerException = null)
        : base(statusCode, reasonPhrase, headers, responseBody, requestId, message, innerException)
    {
    }
}