using System.Text.Json.Serialization;

namespace Opencode.Models.Session;

/// <summary>
/// Describes one persisted session.
/// </summary>
public sealed class SessionInfo
{
    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the session timestamps.
    /// </summary>
    public required SessionTimeInfo Time { get; init; }

    /// <summary>
    /// Gets the current session title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the session schema or API version reported by the service.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the parent session identifier when the session derives from another session.
    /// </summary>
    [JsonPropertyName("parentID")]
    public string? ParentId { get; init; }

    /// <summary>
    /// Gets the current revert metadata when the session is partially reverted.
    /// </summary>
    public SessionRevertInfo? Revert { get; init; }

    /// <summary>
    /// Gets the current share metadata when the session is shared publicly.
    /// </summary>
    public SessionShareInfo? Share { get; init; }
}

/// <summary>
/// Describes the timestamps associated with a session.
/// </summary>
public sealed class SessionTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when the session was created.
    /// </summary>
    public long Created { get; init; }

    /// <summary>
    /// Gets the Unix timestamp for the latest session update.
    /// </summary>
    public long Updated { get; init; }
}

/// <summary>
/// Describes the active revert pointer for a session.
/// </summary>
public sealed class SessionRevertInfo
{
    /// <summary>
    /// Gets the message identifier currently used as the revert point.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the revert diff when the service includes one.
    /// </summary>
    public string? Diff { get; init; }

    /// <summary>
    /// Gets the part identifier associated with the revert when available.
    /// </summary>
    [JsonPropertyName("partID")]
    public string? PartId { get; init; }

    /// <summary>
    /// Gets the snapshot identifier for the revert when available.
    /// </summary>
    public string? Snapshot { get; init; }
}

/// <summary>
/// Describes the public share metadata for a session.
/// </summary>
public sealed class SessionShareInfo
{
    /// <summary>
    /// Gets the public URL for the shared session.
    /// </summary>
    public required string Url { get; init; }
}