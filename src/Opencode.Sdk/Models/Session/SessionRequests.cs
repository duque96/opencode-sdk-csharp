using System.Text.Json.Serialization;

namespace Opencode.Models.Session;

/// <summary>
/// Describes the payload used to send a new chat message to a session.
/// </summary>
public sealed class SessionChatRequest
{
    /// <summary>
    /// Gets the model identifier to use.
    /// </summary>
    [JsonPropertyName("modelID")]
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the message parts to submit.
    /// </summary>
    public required IReadOnlyList<SessionMessagePartInput> Parts { get; init; }

    /// <summary>
    /// Gets the provider identifier to use.
    /// </summary>
    [JsonPropertyName("providerID")]
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the message identifier to assign when provided by the caller.
    /// </summary>
    [JsonPropertyName("messageID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets the execution mode when specified.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mode { get; init; }

    /// <summary>
    /// Gets the system prompt override when specified.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; init; }

    /// <summary>
    /// Gets the tool enablement map when specified.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, bool>? Tools { get; init; }
}

/// <summary>
/// Describes the payload used to initialize a session.
/// </summary>
public sealed class SessionInitRequest
{
    /// <summary>
    /// Gets the message identifier to initialize from.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the model identifier to use.
    /// </summary>
    [JsonPropertyName("modelID")]
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the provider identifier to use.
    /// </summary>
    [JsonPropertyName("providerID")]
    public required string ProviderId { get; init; }
}

/// <summary>
/// Describes the payload used to revert a session.
/// </summary>
public sealed class SessionRevertRequest
{
    /// <summary>
    /// Gets the message identifier that defines the revert point.
    /// </summary>
    [JsonPropertyName("messageID")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the part identifier when reverting to a specific part.
    /// </summary>
    [JsonPropertyName("partID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartId { get; init; }
}

/// <summary>
/// Describes the payload used to summarize a session.
/// </summary>
public sealed class SessionSummarizeRequest
{
    /// <summary>
    /// Gets the model identifier to use.
    /// </summary>
    [JsonPropertyName("modelID")]
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the provider identifier to use.
    /// </summary>
    [JsonPropertyName("providerID")]
    public required string ProviderId { get; init; }
}

/// <summary>
/// Represents a message-part input for a chat request.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(TextPartInput), "text")]
[JsonDerivedType(typeof(FilePartInput), "file")]
public abstract class SessionMessagePartInput
{
    /// <summary>
    /// Gets the part discriminator.
    /// </summary>
    [JsonIgnore]
    public abstract string InputType { get; }

    /// <summary>
    /// Gets the part identifier when supplied by the caller.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }
}

/// <summary>
/// Describes a text-part input.
/// </summary>
public sealed class TextPartInput : SessionMessagePartInput
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string InputType => "text";

    /// <summary>
    /// Gets the text to submit.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the text is synthetic.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Synthetic { get; init; }

    /// <summary>
    /// Gets the optional timing metadata.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextPartTimeInfo? Time { get; init; }
}

/// <summary>
/// Describes a file-part input.
/// </summary>
public sealed class FilePartInput : SessionMessagePartInput
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string InputType => "file";

    /// <summary>
    /// Gets the MIME type of the file.
    /// </summary>
    public required string Mime { get; init; }

    /// <summary>
    /// Gets the file URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the original filename when available.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filename { get; init; }

    /// <summary>
    /// Gets the file source metadata when available.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FilePartSource? Source { get; init; }
}