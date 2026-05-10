using System.Text.Json;
using System.Text.Json.Serialization;
using Opencode.Models.Session;

namespace Opencode.Models.Events;

/// <summary>
/// Represents one item emitted by the server event stream.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(ServerConnectedEvent), "server.connected")]
[JsonDerivedType(typeof(ServerHeartbeatEvent), "server.heartbeat")]
[JsonDerivedType(typeof(InstallationUpdatedEvent), "installation.updated")]
[JsonDerivedType(typeof(LspClientDiagnosticsEvent), "lsp.client.diagnostics")]
[JsonDerivedType(typeof(MessageUpdatedEvent), "message.updated")]
[JsonDerivedType(typeof(MessageRemovedEvent), "message.removed")]
[JsonDerivedType(typeof(MessagePartDeltaEvent), "message.part.delta")]
[JsonDerivedType(typeof(MessagePartUpdatedEvent), "message.part.updated")]
[JsonDerivedType(typeof(MessagePartRemovedEvent), "message.part.removed")]
[JsonDerivedType(typeof(StorageWriteEvent), "storage.write")]
[JsonDerivedType(typeof(PermissionUpdatedEvent), "permission.updated")]
[JsonDerivedType(typeof(FileEditedEvent), "file.edited")]
[JsonDerivedType(typeof(SessionStatusEvent), "session.status")]
[JsonDerivedType(typeof(SessionUpdatedEvent), "session.updated")]
[JsonDerivedType(typeof(SessionDeletedEvent), "session.deleted")]
[JsonDerivedType(typeof(SessionIdleEvent), "session.idle")]
[JsonDerivedType(typeof(SessionErrorEvent), "session.error")]
[JsonDerivedType(typeof(FileWatcherUpdatedEvent), "file.watcher.updated")]
[JsonDerivedType(typeof(IdeInstalledEvent), "ide.installed")]
public abstract class EventStreamItem
{
    private static readonly HashSet<string> KnownEventTypes =
    [
        "server.connected",
        "server.heartbeat",
        "installation.updated",
        "lsp.client.diagnostics",
        "message.updated",
        "message.removed",
        "message.part.delta",
        "message.part.updated",
        "message.part.removed",
        "storage.write",
        "permission.updated",
        "file.edited",
        "session.status",
        "session.updated",
        "session.deleted",
        "session.idle",
        "session.error",
        "file.watcher.updated",
        "ide.installed",
    ];

    /// <summary>
    /// Gets the upstream event discriminator.
    /// </summary>
    [JsonIgnore]
    public abstract string EventType { get; }

    internal static bool IsKnownEventType(string? eventType)
    {
        return !string.IsNullOrWhiteSpace(eventType) && KnownEventTypes.Contains(eventType);
    }
}

/// <summary>
/// Describes the initial connection event emitted when the server accepts an SSE subscription.
/// </summary>
public sealed class ServerConnectedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "server.connected";

    /// <summary>
    /// Gets the payload emitted by the server for the connection event.
    /// </summary>
    public JsonElement? Properties { get; init; }
}

/// <summary>
/// Describes the heartbeat event emitted periodically by the server while the SSE subscription stays open.
/// </summary>
public sealed class ServerHeartbeatEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "server.heartbeat";

    /// <summary>
    /// Gets the payload emitted by the server for the heartbeat event.
    /// </summary>
    public JsonElement? Properties { get; init; }
}

/// <summary>
/// Describes a runtime installation update for the service.
/// </summary>
public sealed class InstallationUpdatedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "installation.updated";

    /// <summary>
    /// Gets the installation details reported by the service.
    /// </summary>
    public required InstallationUpdatedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the installation metadata carried by an installation update event.
/// </summary>
public sealed class InstallationUpdatedEventProperties
{
    /// <summary>
    /// Gets the installed service version.
    /// </summary>
    public required string Version { get; init; }
}

/// <summary>
/// Describes an LSP diagnostics refresh emitted for one file.
/// </summary>
public sealed class LspClientDiagnosticsEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "lsp.client.diagnostics";

    /// <summary>
    /// Gets the diagnostics event payload.
    /// </summary>
    public required LspClientDiagnosticsEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for an LSP diagnostics event.
/// </summary>
public sealed class LspClientDiagnosticsEventProperties
{
    /// <summary>
    /// Gets the path whose diagnostics changed.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the language server identifier.
    /// </summary>
    [JsonPropertyName("serverID")]
    public required string ServerId { get; init; }
}

/// <summary>
/// Describes an update to one session message.
/// </summary>
public sealed class MessageUpdatedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "message.updated";

    /// <summary>
    /// Gets the updated message payload.
    /// </summary>
    public required MessageUpdatedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a message update event.
/// </summary>
public sealed class MessageUpdatedEventProperties
{
    /// <summary>
    /// Gets the updated message metadata.
    /// </summary>
    public required SessionMessage Info { get; init; }
}

/// <summary>
/// Describes the removal of one message from a session timeline.
/// </summary>
public sealed class MessageRemovedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "message.removed";

    /// <summary>
    /// Gets the removed message payload.
    /// </summary>
    public required MessageRemovedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a message removal event.
/// </summary>
public sealed class MessageRemovedEventProperties
{
    /// <summary>
    /// Gets the removed message identifier.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the session identifier that owned the removed message.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Describes a delta emitted for one message part while the server is still streaming it.
/// </summary>
public sealed class MessagePartDeltaEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "message.part.delta";

    /// <summary>
    /// Gets the incremental part payload.
    /// </summary>
    public required MessagePartDeltaEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a message part delta event.
/// </summary>
public sealed class MessagePartDeltaEventProperties
{
    /// <summary>
    /// Gets the session identifier that owns the streamed message part.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the owning message identifier.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the streamed message part identifier.
    /// </summary>
    [JsonPropertyName("partID")]
    public required string PartId { get; init; }

    /// <summary>
    /// Gets the field receiving the incremental update.
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Gets the emitted delta content.
    /// </summary>
    public required string Delta { get; init; }
}

/// <summary>
/// Describes an update to one message part.
/// </summary>
public sealed class MessagePartUpdatedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "message.part.updated";

    /// <summary>
    /// Gets the updated part payload.
    /// </summary>
    public required MessagePartUpdatedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a message part update event.
/// </summary>
public sealed class MessagePartUpdatedEventProperties
{
    /// <summary>
    /// Gets the updated message part.
    /// </summary>
    public required MessagePart Part { get; init; }
}

/// <summary>
/// Describes the removal of one message part.
/// </summary>
public sealed class MessagePartRemovedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "message.part.removed";

    /// <summary>
    /// Gets the removed part payload.
    /// </summary>
    public required MessagePartRemovedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a message part removal event.
/// </summary>
public sealed class MessagePartRemovedEventProperties
{
    /// <summary>
    /// Gets the owning message identifier.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the removed part identifier.
    /// </summary>
    [JsonPropertyName("partID")]
    public required string PartId { get; init; }
}

/// <summary>
/// Describes a storage write performed by the service.
/// </summary>
public sealed class StorageWriteEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "storage.write";

    /// <summary>
    /// Gets the storage write payload.
    /// </summary>
    public required StorageWriteEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a storage write event.
/// </summary>
public sealed class StorageWriteEventProperties
{
    /// <summary>
    /// Gets the storage key that changed.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the written content when the service includes it.
    /// </summary>
    public JsonElement? Content { get; init; }
}

/// <summary>
/// Describes a permission state update.
/// </summary>
public sealed class PermissionUpdatedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "permission.updated";

    /// <summary>
    /// Gets the permission update payload.
    /// </summary>
    public required PermissionUpdatedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a permission update event.
/// </summary>
public sealed class PermissionUpdatedEventProperties
{
    /// <summary>
    /// Gets the permission identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the permission metadata bag.
    /// </summary>
    public required IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }

    /// <summary>
    /// Gets the session identifier associated with the permission.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the permission timing metadata.
    /// </summary>
    public required PermissionUpdatedEventTimeInfo Time { get; init; }

    /// <summary>
    /// Gets the user-facing permission title.
    /// </summary>
    public required string Title { get; init; }
}

/// <summary>
/// Describes the timing metadata for a permission update event.
/// </summary>
public sealed class PermissionUpdatedEventTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when the permission was created.
    /// </summary>
    public long Created { get; init; }
}

/// <summary>
/// Describes one file edit notification.
/// </summary>
public sealed class FileEditedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "file.edited";

    /// <summary>
    /// Gets the file edit payload.
    /// </summary>
    public required FileEditedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a file edit event.
/// </summary>
public sealed class FileEditedEventProperties
{
    /// <summary>
    /// Gets the edited file path.
    /// </summary>
    public required string File { get; init; }
}

/// <summary>
/// Describes a session status transition notification.
/// </summary>
public sealed class SessionStatusEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "session.status";

    /// <summary>
    /// Gets the session status payload.
    /// </summary>
    public required SessionStatusEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a session status event.
/// </summary>
public sealed class SessionStatusEventProperties
{
    /// <summary>
    /// Gets the session identifier associated with the status update.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the status object emitted by the server.
    /// </summary>
    public required SessionStatusInfo Status { get; init; }
}

/// <summary>
/// Describes one session status value emitted by the server.
/// </summary>
public sealed class SessionStatusInfo
{
    /// <summary>
    /// Gets the status discriminator.
    /// </summary>
    public required string Type { get; init; }
}

/// <summary>
/// Describes a session metadata update.
/// </summary>
public sealed class SessionUpdatedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "session.updated";

    /// <summary>
    /// Gets the updated session payload.
    /// </summary>
    public required SessionUpdatedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a session update event.
/// </summary>
public sealed class SessionUpdatedEventProperties
{
    /// <summary>
    /// Gets the updated session information.
    /// </summary>
    public required SessionInfo Info { get; init; }
}

/// <summary>
/// Describes a session deletion event.
/// </summary>
public sealed class SessionDeletedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "session.deleted";

    /// <summary>
    /// Gets the deleted session payload.
    /// </summary>
    public required SessionDeletedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a session deletion event.
/// </summary>
public sealed class SessionDeletedEventProperties
{
    /// <summary>
    /// Gets the deleted session information.
    /// </summary>
    public required SessionInfo Info { get; init; }
}

/// <summary>
/// Describes a session idle notification.
/// </summary>
public sealed class SessionIdleEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "session.idle";

    /// <summary>
    /// Gets the idle notification payload.
    /// </summary>
    public required SessionIdleEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a session idle event.
/// </summary>
public sealed class SessionIdleEventProperties
{
    /// <summary>
    /// Gets the idle session identifier.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Describes a domain error emitted as part of the normal session event stream.
/// </summary>
public sealed class SessionErrorEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "session.error";

    /// <summary>
    /// Gets the session error payload.
    /// </summary>
    public required SessionErrorEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a session error event.
/// </summary>
public sealed class SessionErrorEventProperties
{
    /// <summary>
    /// Gets the embedded domain error when the service includes one.
    /// </summary>
    public AssistantMessageError? Error { get; init; }

    /// <summary>
    /// Gets the related session identifier when the service includes one.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public string? SessionId { get; init; }
}

/// <summary>
/// Describes a file watcher change notification.
/// </summary>
public sealed class FileWatcherUpdatedEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "file.watcher.updated";

    /// <summary>
    /// Gets the file watcher payload.
    /// </summary>
    public required FileWatcherUpdatedEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for a file watcher update event.
/// </summary>
public sealed class FileWatcherUpdatedEventProperties
{
    /// <summary>
    /// Gets the watcher event name.
    /// </summary>
    public required string Event { get; init; }

    /// <summary>
    /// Gets the file path associated with the watcher event.
    /// </summary>
    public required string File { get; init; }
}

/// <summary>
/// Describes an IDE installation notification.
/// </summary>
public sealed class IdeInstalledEvent : EventStreamItem
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string EventType => "ide.installed";

    /// <summary>
    /// Gets the IDE installation payload.
    /// </summary>
    public required IdeInstalledEventProperties Properties { get; init; }
}

/// <summary>
/// Describes the payload for an IDE installation event.
/// </summary>
public sealed class IdeInstalledEventProperties
{
    /// <summary>
    /// Gets the installed IDE identifier.
    /// </summary>
    public required string Ide { get; init; }
}