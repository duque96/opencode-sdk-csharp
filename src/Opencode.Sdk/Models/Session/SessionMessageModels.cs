using System.Text.Json;
using System.Text.Json.Serialization;

namespace Opencode.Models.Session;

/// <summary>
/// Describes one session message and its associated parts.
/// </summary>
public sealed class SessionMessageEntry
{
    /// <summary>
    /// Gets the message metadata.
    /// </summary>
    public required SessionMessage Info { get; init; }

    /// <summary>
    /// Gets the parts emitted for the message.
    /// </summary>
    public required IReadOnlyList<MessagePart> Parts { get; init; }
}

/// <summary>
/// Represents a session message.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "role", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(UserMessage), "user")]
[JsonDerivedType(typeof(AssistantMessage), "assistant")]
public abstract class SessionMessage
{
    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the role discriminator for the message.
    /// </summary>
    [JsonIgnore]
    public abstract string MessageRole { get; }

    /// <summary>
    /// Gets the session identifier that owns the message.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Describes a user-authored message.
/// </summary>
public sealed class UserMessage : SessionMessage
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string MessageRole => "user";

    /// <summary>
    /// Gets the user message timestamps.
    /// </summary>
    public required UserMessageTimeInfo Time { get; init; }
}

/// <summary>
/// Describes the timestamps associated with a user message.
/// </summary>
public sealed class UserMessageTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when the message was created.
    /// </summary>
    public long Created { get; init; }
}

/// <summary>
/// Describes an assistant-authored message.
/// </summary>
public sealed class AssistantMessage : SessionMessage
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string MessageRole => "assistant";

    /// <summary>
    /// Gets the parent message identifier when the assistant reply belongs to a thread.
    /// </summary>
    [JsonPropertyName("parentID")]
    public string? ParentId { get; init; }

    /// <summary>
    /// Gets the selected agent when the server includes it.
    /// </summary>
    public string? Agent { get; init; }

    /// <summary>
    /// Gets the cost attributed to the assistant response.
    /// </summary>
    public double Cost { get; init; }

    /// <summary>
    /// Gets the mode used to generate the response.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// Gets the model identifier used to generate the response.
    /// </summary>
    [JsonPropertyName("modelID")]
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the path metadata associated with the response.
    /// </summary>
    public required AssistantMessagePathInfo Path { get; init; }

    /// <summary>
    /// Gets the provider identifier used to generate the response.
    /// </summary>
    [JsonPropertyName("providerID")]
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the system prompts applied to the response.
    /// </summary>
    public IReadOnlyList<string>? System { get; init; }

    /// <summary>
    /// Gets the assistant message timestamps.
    /// </summary>
    public required AssistantMessageTimeInfo Time { get; init; }

    /// <summary>
    /// Gets the token accounting for the response.
    /// </summary>
    public required AssistantMessageTokensInfo Tokens { get; init; }

    /// <summary>
    /// Gets the assistant-level error embedded in the payload when present.
    /// </summary>
    public AssistantMessageError? Error { get; init; }

    /// <summary>
    /// Gets the model finish reason when the server includes it.
    /// </summary>
    public string? Finish { get; init; }

    /// <summary>
    /// Gets a value indicating whether the message represents a generated summary.
    /// </summary>
    public bool? Summary { get; init; }
}

/// <summary>
/// Describes the filesystem path context for an assistant message.
/// </summary>
public sealed class AssistantMessagePathInfo
{
    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    public required string Cwd { get; init; }

    /// <summary>
    /// Gets the workspace root directory.
    /// </summary>
    public required string Root { get; init; }
}

/// <summary>
/// Describes the timestamps associated with an assistant message.
/// </summary>
public sealed class AssistantMessageTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when message generation started.
    /// </summary>
    public long Created { get; init; }

    /// <summary>
    /// Gets the Unix timestamp for when message generation completed, when available.
    /// </summary>
    public long? Completed { get; init; }
}

/// <summary>
/// Describes token accounting for an assistant message.
/// </summary>
public sealed class AssistantMessageTokensInfo
{
    /// <summary>
    /// Gets the total token count when the server includes it.
    /// </summary>
    public int? Total { get; init; }

    /// <summary>
    /// Gets the cache token counters.
    /// </summary>
    public required MessageCacheTokensInfo Cache { get; init; }

    /// <summary>
    /// Gets the number of input tokens used.
    /// </summary>
    public int Input { get; init; }

    /// <summary>
    /// Gets the number of output tokens produced.
    /// </summary>
    public int Output { get; init; }

    /// <summary>
    /// Gets the number of reasoning tokens used.
    /// </summary>
    public int Reasoning { get; init; }
}

/// <summary>
/// Describes cache token counters.
/// </summary>
public sealed class MessageCacheTokensInfo
{
    /// <summary>
    /// Gets the number of cache read tokens.
    /// </summary>
    public int Read { get; init; }

    /// <summary>
    /// Gets the number of cache write tokens.
    /// </summary>
    public int Write { get; init; }
}

/// <summary>
/// Represents a message part.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(TextPart), "text")]
[JsonDerivedType(typeof(ReasoningPart), "reasoning")]
[JsonDerivedType(typeof(FilePart), "file")]
[JsonDerivedType(typeof(ToolPart), "tool")]
[JsonDerivedType(typeof(StepStartPart), "step-start")]
[JsonDerivedType(typeof(StepFinishPart), "step-finish")]
[JsonDerivedType(typeof(SnapshotPart), "snapshot")]
[JsonDerivedType(typeof(PatchPart), "patch")]
public abstract class MessagePart
{
    /// <summary>
    /// Gets the part identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the owning message identifier.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the owning session identifier.
    /// </summary>
    [JsonPropertyName("sessionID")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the part discriminator.
    /// </summary>
    [JsonIgnore]
    public abstract string PartType { get; }
}

/// <summary>
/// Describes a text message part.
/// </summary>
public sealed class TextPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "text";

    /// <summary>
    /// Gets the text emitted for the part.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the text was synthesized locally by the service.
    /// </summary>
    public bool? Synthetic { get; init; }

    /// <summary>
    /// Gets the timing information for the emitted text when available.
    /// </summary>
    public TextPartTimeInfo? Time { get; init; }
}

/// <summary>
/// Describes a reasoning message part.
/// </summary>
public sealed class ReasoningPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "reasoning";

    /// <summary>
    /// Gets the reasoning text emitted for the part.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the timing information for the emitted reasoning when available.
    /// </summary>
    public TextPartTimeInfo? Time { get; init; }

    /// <summary>
    /// Gets provider-specific metadata when the server includes it.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }
}

/// <summary>
/// Describes the timing information for a text part.
/// </summary>
public sealed class TextPartTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when text emission started.
    /// </summary>
    public long Start { get; init; }

    /// <summary>
    /// Gets the Unix timestamp for when text emission ended, when available.
    /// </summary>
    public long? End { get; init; }
}

/// <summary>
/// Describes a file attachment part.
/// </summary>
public sealed class FilePart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "file";

    /// <summary>
    /// Gets the MIME type of the attached file.
    /// </summary>
    public required string Mime { get; init; }

    /// <summary>
    /// Gets the file URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the original filename when available.
    /// </summary>
    public string? Filename { get; init; }

    /// <summary>
    /// Gets the file source metadata when the server includes it.
    /// </summary>
    public FilePartSource? Source { get; init; }
}

/// <summary>
/// Describes a tool invocation part.
/// </summary>
public sealed class ToolPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "tool";

    /// <summary>
    /// Gets the tool call identifier.
    /// </summary>
    [JsonPropertyName("callID")]
    public required string CallId { get; init; }

    /// <summary>
    /// Gets the current tool execution state.
    /// </summary>
    public required ToolState State { get; init; }

    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public required string Tool { get; init; }
}

/// <summary>
/// Describes the start of a tool execution step.
/// </summary>
public sealed class StepStartPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "step-start";
}

/// <summary>
/// Describes the completion of a tool execution step.
/// </summary>
public sealed class StepFinishPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "step-finish";

    /// <summary>
    /// Gets the cost attributed to the step.
    /// </summary>
    public double Cost { get; init; }

    /// <summary>
    /// Gets the finish reason when the server includes it.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the token accounting for the step.
    /// </summary>
    public required AssistantMessageTokensInfo Tokens { get; init; }
}

/// <summary>
/// Describes a snapshot part.
/// </summary>
public sealed class SnapshotPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "snapshot";

    /// <summary>
    /// Gets the snapshot payload.
    /// </summary>
    public required string Snapshot { get; init; }
}

/// <summary>
/// Describes a patch part.
/// </summary>
public sealed class PatchPart : MessagePart
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string PartType => "patch";

    /// <summary>
    /// Gets the files affected by the patch.
    /// </summary>
    public required IReadOnlyList<string> Files { get; init; }

    /// <summary>
    /// Gets the patch hash.
    /// </summary>
    public required string Hash { get; init; }
}

/// <summary>
/// Represents a file-source discriminator for a file part.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(FileSource), "file")]
[JsonDerivedType(typeof(SymbolSource), "symbol")]
public abstract class FilePartSource
{
    /// <summary>
    /// Gets the source discriminator.
    /// </summary>
    [JsonIgnore]
    public abstract string SourceType { get; }
}

/// <summary>
/// Describes text extracted from a file source.
/// </summary>
public sealed class FilePartSourceText
{
    /// <summary>
    /// Gets the ending offset for the extracted text.
    /// </summary>
    public int End { get; init; }

    /// <summary>
    /// Gets the starting offset for the extracted text.
    /// </summary>
    public int Start { get; init; }

    /// <summary>
    /// Gets the extracted text value.
    /// </summary>
    public required string Value { get; init; }
}

/// <summary>
/// Describes a file-based source for an attached file.
/// </summary>
public sealed class FileSource : FilePartSource
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string SourceType => "file";

    /// <summary>
    /// Gets the workspace-relative path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the extracted text block.
    /// </summary>
    public required FilePartSourceText Text { get; init; }
}

/// <summary>
/// Describes a symbol-based source for an attached file.
/// </summary>
public sealed class SymbolSource : FilePartSource
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string SourceType => "symbol";

    /// <summary>
    /// Gets the symbol kind.
    /// </summary>
    public int Kind { get; init; }

    /// <summary>
    /// Gets the symbol name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the file path containing the symbol.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the source range for the symbol.
    /// </summary>
    public required SymbolSourceRangeInfo Range { get; init; }

    /// <summary>
    /// Gets the extracted text block.
    /// </summary>
    public required FilePartSourceText Text { get; init; }
}

/// <summary>
/// Describes a symbol source range.
/// </summary>
public sealed class SymbolSourceRangeInfo
{
    /// <summary>
    /// Gets the end position.
    /// </summary>
    public required SymbolSourcePositionInfo End { get; init; }

    /// <summary>
    /// Gets the start position.
    /// </summary>
    public required SymbolSourcePositionInfo Start { get; init; }
}

/// <summary>
/// Describes a line and character position.
/// </summary>
public sealed class SymbolSourcePositionInfo
{
    /// <summary>
    /// Gets the zero-based character offset.
    /// </summary>
    public int Character { get; init; }

    /// <summary>
    /// Gets the zero-based line number.
    /// </summary>
    public int Line { get; init; }
}

/// <summary>
/// Represents the state of a tool execution.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "status", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(ToolStatePending), "pending")]
[JsonDerivedType(typeof(ToolStateRunning), "running")]
[JsonDerivedType(typeof(ToolStateCompleted), "completed")]
[JsonDerivedType(typeof(ToolStateError), "error")]
public abstract class ToolState
{
    /// <summary>
    /// Gets the tool state discriminator.
    /// </summary>
    [JsonIgnore]
    public abstract string ToolStatus { get; }
}

/// <summary>
/// Describes a pending tool state.
/// </summary>
public sealed class ToolStatePending : ToolState
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ToolStatus => "pending";
}

/// <summary>
/// Describes a running tool state.
/// </summary>
public sealed class ToolStateRunning : ToolState
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ToolStatus => "running";

    /// <summary>
    /// Gets the tool timing information.
    /// </summary>
    public required ToolStateRunningTimeInfo Time { get; init; }

    /// <summary>
    /// Gets the tool input payload when the service includes it.
    /// </summary>
    public JsonElement? Input { get; init; }

    /// <summary>
    /// Gets the tool metadata when the service includes it.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }

    /// <summary>
    /// Gets the display title for the running tool when available.
    /// </summary>
    public string? Title { get; init; }
}

/// <summary>
/// Describes the timing information for a running tool.
/// </summary>
public sealed class ToolStateRunningTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when tool execution started.
    /// </summary>
    public long Start { get; init; }
}

/// <summary>
/// Describes a completed tool state.
/// </summary>
public sealed class ToolStateCompleted : ToolState
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ToolStatus => "completed";

    /// <summary>
    /// Gets the tool input payload.
    /// </summary>
    public required IReadOnlyDictionary<string, JsonElement> Input { get; init; }

    /// <summary>
    /// Gets the tool metadata payload.
    /// </summary>
    public required IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }

    /// <summary>
    /// Gets the textual output returned by the tool.
    /// </summary>
    public required string Output { get; init; }

    /// <summary>
    /// Gets the tool timing information.
    /// </summary>
    public required ToolStateCompletedTimeInfo Time { get; init; }

    /// <summary>
    /// Gets the display title for the completed tool.
    /// </summary>
    public required string Title { get; init; }
}

/// <summary>
/// Describes the timing information for a completed or failed tool.
/// </summary>
public sealed class ToolStateCompletedTimeInfo
{
    /// <summary>
    /// Gets the Unix timestamp for when tool execution ended.
    /// </summary>
    public long End { get; init; }

    /// <summary>
    /// Gets the Unix timestamp for when tool execution started.
    /// </summary>
    public long Start { get; init; }
}

/// <summary>
/// Describes a failed tool state.
/// </summary>
public sealed class ToolStateError : ToolState
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ToolStatus => "error";

    /// <summary>
    /// Gets the error text returned by the tool.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Gets the tool input payload.
    /// </summary>
    public required IReadOnlyDictionary<string, JsonElement> Input { get; init; }

    /// <summary>
    /// Gets the tool timing information.
    /// </summary>
    public required ToolStateCompletedTimeInfo Time { get; init; }
}

/// <summary>
/// Represents an assistant-level payload error embedded in a successful response.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "name", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(ProviderAuthError), "ProviderAuthError")]
[JsonDerivedType(typeof(UnknownError), "UnknownError")]
[JsonDerivedType(typeof(MessageAbortedError), "MessageAbortedError")]
[JsonDerivedType(typeof(MessageOutputLengthError), "MessageOutputLengthError")]
public abstract class AssistantMessageError
{
    /// <summary>
    /// Gets the error discriminator.
    /// </summary>
    [JsonIgnore]
    public abstract string ErrorName { get; }
}

/// <summary>
/// Describes a provider authentication error embedded in an assistant payload.
/// </summary>
public sealed class ProviderAuthError : AssistantMessageError
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ErrorName => "ProviderAuthError";

    /// <summary>
    /// Gets the provider authentication error data.
    /// </summary>
    public required ProviderAuthErrorData Data { get; init; }
}

/// <summary>
/// Describes the provider authentication error payload.
/// </summary>
public sealed class ProviderAuthErrorData
{
    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the provider identifier that triggered the error.
    /// </summary>
    [JsonPropertyName("providerID")]
    public required string ProviderId { get; init; }
}

/// <summary>
/// Describes an unknown assistant payload error.
/// </summary>
public sealed class UnknownError : AssistantMessageError
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ErrorName => "UnknownError";

    /// <summary>
    /// Gets the unknown error data.
    /// </summary>
    public required UnknownErrorData Data { get; init; }
}

/// <summary>
/// Describes the unknown error payload.
/// </summary>
public sealed class UnknownErrorData
{
    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Describes an aborted assistant payload error.
/// </summary>
public sealed class MessageAbortedError : AssistantMessageError
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ErrorName => "MessageAbortedError";

    /// <summary>
    /// Gets the opaque error data returned by the service.
    /// </summary>
    public JsonElement Data { get; init; }
}

/// <summary>
/// Describes an assistant output-length error.
/// </summary>
public sealed class MessageOutputLengthError : AssistantMessageError
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string ErrorName => "MessageOutputLengthError";

    /// <summary>
    /// Gets the opaque error data returned by the service.
    /// </summary>
    public JsonElement Data { get; init; }
}