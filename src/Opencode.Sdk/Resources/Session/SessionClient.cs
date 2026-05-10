using Opencode.Client;
using Opencode.Errors;
using Opencode.Internal.Http;
using Opencode.Models.Session;

namespace Opencode.Resources.Session;

/// <summary>
/// Provides access to session operations.
/// </summary>
public sealed class SessionClient
{
    private readonly OpencodeClient _client;

    internal SessionClient(OpencodeClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Creates a new session.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The created session metadata.</returns>
    public Task<SessionInfo?> CreateAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<SessionInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/session",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Lists all known sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The sessions returned by the API.</returns>
    public Task<SessionInfo[]?> ListAsync(CancellationToken cancellationToken = default)
    {
        return _client.Pipeline.SendAsync<SessionInfo[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/session",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Deletes a session and its persisted data.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the session was deleted.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is empty or whitespace.</exception>
    public Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Delete,
            Path = $"/session/{sessionId}",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Aborts the currently running work for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the abort request was accepted.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is empty or whitespace.</exception>
    public Task<bool> AbortAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/abort",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Creates and sends a new user message to a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The chat payload to submit.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The assistant message returned by the API.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when required values are missing.</exception>
    public async Task<AssistantMessage?> ChatAsync(string sessionId, SessionChatRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);
        ValidateChatRequest(request);

        var response = await _client.Pipeline.SendAsync<SessionMessageEntry>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/message",
            Body = request,
            CancellationToken = cancellationToken,
        });

        if (response is null)
        {
            return null;
        }

        if (response.Info is AssistantMessage assistantMessage)
        {
            return assistantMessage;
        }

        throw new UnexpectedResponseException("The server returned a chat payload that did not contain an assistant message.");
    }

    /// <summary>
    /// Sends a new user message to a session without waiting for the assistant response.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The chat payload to submit.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the prompt request was accepted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when required values are missing.</exception>
    public async Task<bool> PromptAsync(string sessionId, SessionChatRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);
        ValidateChatRequest(request);

        await _client.Pipeline.SendAsync<object>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/prompt_async",
            Body = request,
            CancellationToken = cancellationToken,
        });

        return true;
    }

    /// <summary>
    /// Analyzes the app and initializes session-specific guidance.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The initialization payload to submit.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when initialization completed successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when required values are missing.</exception>
    public Task<bool> InitAsync(string sessionId, SessionInitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);
        ValidateInitRequest(request);

        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/init",
            Body = request,
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Gets all messages recorded for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The session messages and their parts.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is empty or whitespace.</exception>
    public Task<SessionMessageEntry[]?> MessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        return _client.Pipeline.SendAsync<SessionMessageEntry[]>(new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = $"/session/{sessionId}/message",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Reverts a message within the session timeline.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The revert payload to submit.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The updated session metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when required values are missing.</exception>
    public Task<SessionInfo?> RevertAsync(string sessionId, SessionRevertRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);
        ValidateRevertRequest(request);

        return _client.Pipeline.SendAsync<SessionInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/revert",
            Body = request,
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Shares a session and returns the updated metadata.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The updated session metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is empty or whitespace.</exception>
    public Task<SessionInfo?> ShareAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        return _client.Pipeline.SendAsync<SessionInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/share",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Summarizes the current session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The summarization payload to submit.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns><see langword="true"/> when the summary request completed successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when required values are missing.</exception>
    public Task<bool> SummarizeAsync(string sessionId, SessionSummarizeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);
        ValidateSummarizeRequest(request);

        return _client.Pipeline.SendAsync<bool>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/summarize",
            Body = request,
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Restores all reverted messages for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The updated session metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is empty or whitespace.</exception>
    public Task<SessionInfo?> UnrevertAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        return _client.Pipeline.SendAsync<SessionInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = $"/session/{sessionId}/unrevert",
            CancellationToken = cancellationToken,
        });
    }

    /// <summary>
    /// Removes the public share for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the request to complete.</param>
    /// <returns>The updated session metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is empty or whitespace.</exception>
    public Task<SessionInfo?> UnshareAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ValidateSessionId(sessionId);

        return _client.Pipeline.SendAsync<SessionInfo>(new HttpPipelineRequest
        {
            Method = HttpMethod.Delete,
            Path = $"/session/{sessionId}/share",
            CancellationToken = cancellationToken,
        });
    }

    private static void ValidateSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("The session ID must not be empty.", nameof(sessionId));
        }
    }

    private static void ValidateChatRequest(SessionChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new ArgumentException("The model ID must not be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ProviderId))
        {
            throw new ArgumentException("The provider ID must not be empty.", nameof(request));
        }

        if (request.Parts is null || request.Parts.Count == 0)
        {
            throw new ArgumentException("The chat request must contain at least one part.", nameof(request));
        }
    }

    private static void ValidateInitRequest(SessionInitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            throw new ArgumentException("The message ID must not be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new ArgumentException("The model ID must not be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ProviderId))
        {
            throw new ArgumentException("The provider ID must not be empty.", nameof(request));
        }
    }

    private static void ValidateRevertRequest(SessionRevertRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            throw new ArgumentException("The message ID must not be empty.", nameof(request));
        }
    }

    private static void ValidateSummarizeRequest(SessionSummarizeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new ArgumentException("The model ID must not be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ProviderId))
        {
            throw new ArgumentException("The provider ID must not be empty.", nameof(request));
        }
    }
}