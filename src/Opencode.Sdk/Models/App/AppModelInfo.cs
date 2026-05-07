using System.Text.Json;
using System.Text.Json.Serialization;

namespace Opencode.Models.App;

/// <summary>
/// Describes one model offered by a provider.
/// </summary>
public sealed class AppModelInfo
{
    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports attachments.
    /// </summary>
    public bool Attachment { get; init; }

    /// <summary>
    /// Gets the model cost metadata.
    /// </summary>
    public required AppModelCostInfo Cost { get; init; }

    /// <summary>
    /// Gets the model token limits.
    /// </summary>
    public required AppModelLimitInfo Limit { get; init; }

    /// <summary>
    /// Gets the display name of the model.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets provider-specific model options when present.
    /// </summary>
    public Dictionary<string, JsonElement>? Options { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports reasoning.
    /// </summary>
    public bool Reasoning { get; init; }

    /// <summary>
    /// Gets the model release date string returned by the API.
    /// </summary>
    [JsonPropertyName("release_date")]
    public required string ReleaseDate { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports configurable temperature.
    /// </summary>
    public bool Temperature { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports tool calls.
    /// </summary>
    [JsonPropertyName("tool_call")]
    public bool ToolCall { get; init; }
}

/// <summary>
/// Describes the pricing metadata for a model.
/// </summary>
public sealed class AppModelCostInfo
{
    /// <summary>
    /// Gets the input token cost.
    /// </summary>
    public decimal Input { get; init; }

    /// <summary>
    /// Gets the output token cost.
    /// </summary>
    public decimal Output { get; init; }

    /// <summary>
    /// Gets the cache read cost when available.
    /// </summary>
    [JsonPropertyName("cache_read")]
    public decimal? CacheRead { get; init; }

    /// <summary>
    /// Gets the cache write cost when available.
    /// </summary>
    [JsonPropertyName("cache_write")]
    public decimal? CacheWrite { get; init; }
}

/// <summary>
/// Describes the token limits for a model.
/// </summary>
public sealed class AppModelLimitInfo
{
    /// <summary>
    /// Gets the context window size.
    /// </summary>
    public int Context { get; init; }

    /// <summary>
    /// Gets the maximum output token count.
    /// </summary>
    public int Output { get; init; }
}