# Opencode SDK .NET Event Catalog

This document is the central reference for the server-sent events currently supported by `Opencode.Sdk` through `EventClient.ListAsync`.

The public entry point is `IAsyncEnumerable<EventStreamItem>`. Each SSE payload is deserialized into one of the event types listed below when the upstream `type` discriminator matches a known event.

Unknown event types are ignored by the event stream instead of terminating enumeration. That keeps the SDK resilient to forward-compatible control events emitted by the server.

## Consume Events

```csharp
using Opencode.Client;
using Opencode.Models.Events;

var client = new OpencodeClient(new OpencodeClientOptions());
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await foreach (var item in client.Event.ListAsync(cancellationTokenSource.Token))
{
    switch (item)
    {
        case ServerConnectedEvent:
            Console.WriteLine("Connected to the event stream.");
            break;
        case ServerHeartbeatEvent:
            Console.WriteLine("Heartbeat received.");
            break;
        case MessageUpdatedEvent messageUpdatedEvent:
            Console.WriteLine($"Message updated: {messageUpdatedEvent.Properties.Info.Id}");
            break;
        case SessionIdleEvent sessionIdleEvent:
            Console.WriteLine($"Session idle: {sessionIdleEvent.Properties.SessionId}");
            break;
        default:
            Console.WriteLine(item.EventType);
            break;
    }
}
```

## Supported Events

| SSE `type` | .NET type | Payload summary |
| --- | --- | --- |
| `server.connected` | `ServerConnectedEvent` | Control event emitted when the SSE subscription is accepted. `Properties` is currently modeled as `JsonElement?`. |
| `server.heartbeat` | `ServerHeartbeatEvent` | Control event emitted while the SSE subscription remains alive. `Properties` is currently modeled as `JsonElement?`. |
| `installation.updated` | `InstallationUpdatedEvent` | Installation metadata update. Exposes `Properties.Version`. |
| `lsp.client.diagnostics` | `LspClientDiagnosticsEvent` | Diagnostics refresh for one file. Exposes `Properties.Path` and `Properties.ServerId`. |
| `message.updated` | `MessageUpdatedEvent` | Session message metadata update. Exposes `Properties.Info` as a polymorphic `SessionMessage`. |
| `message.removed` | `MessageRemovedEvent` | Message removal notification. Exposes `Properties.MessageId` and `Properties.SessionId`. |
| `message.part.updated` | `MessagePartUpdatedEvent` | Incremental update to one message part. Exposes `Properties.Part` as a polymorphic `MessagePart`. |
| `message.part.removed` | `MessagePartRemovedEvent` | Message part removal notification. Exposes `Properties.MessageId` and `Properties.PartId`. |
| `storage.write` | `StorageWriteEvent` | Storage write notification. Exposes `Properties.Key` and optional `Properties.Content`. |
| `permission.updated` | `PermissionUpdatedEvent` | Permission state update. Exposes `Properties.Id`, `Properties.Metadata`, `Properties.SessionId`, `Properties.Time`, and `Properties.Title`. |
| `file.edited` | `FileEditedEvent` | File edit notification. Exposes `Properties.File`. |
| `session.status` | `SessionStatusEvent` | Session status transition. Exposes `Properties.SessionId` and `Properties.Status.Type` such as `busy`. |
| `session.updated` | `SessionUpdatedEvent` | Session metadata update. Exposes `Properties.Info`. |
| `session.deleted` | `SessionDeletedEvent` | Session deletion notification. Exposes `Properties.Info`. |
| `session.idle` | `SessionIdleEvent` | Session finished processing the current turn. Exposes `Properties.SessionId`. |
| `session.error` | `SessionErrorEvent` | Domain error emitted during session processing. Exposes optional `Properties.Error` and optional `Properties.SessionId`. |
| `file.watcher.updated` | `FileWatcherUpdatedEvent` | File watcher notification. Exposes `Properties.Event` and `Properties.File`. |
| `ide.installed` | `IdeInstalledEvent` | IDE installation notification. Exposes `Properties.Ide`. |

## Related Payload Hierarchies

Some event payloads reference other public polymorphic models rather than plain scalar data:

- `MessageUpdatedEvent.Properties.Info` can be `UserMessage` or `AssistantMessage`.
- `MessagePartUpdatedEvent.Properties.Part` can be `TextPart`, `ReasoningPart`, `FilePart`, `ToolPart`, `StepStartPart`, `StepFinishPart`, `SnapshotPart`, or `PatchPart`.
- `SessionErrorEvent.Properties.Error` can be one of the public `AssistantMessageError` subtypes.

For the authoritative code-level contract, see `src/Opencode.Sdk/Models/Events/EventModels.cs`.

## Maintenance Notes

- When a new supported SSE event is added to `EventStreamItem`, update this document in the same change.
- When a server control event should be tolerated but not modeled, document that decision explicitly in the event stream tests.
- If the server starts sending structured payloads for `server.connected` or `server.heartbeat`, replace the current `JsonElement?` placeholder with typed property classes and update this catalog.