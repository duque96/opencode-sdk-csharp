using System.Text.Json.Serialization;

namespace Opencode.Models.App;

/// <summary>
/// Describes one available application mode.
/// </summary>
public sealed class AppModeInfo
{
    /// <summary>
    /// Gets the mode name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the tool availability map for the mode.
    /// </summary>
    public required Dictionary<string, bool> Tools { get; init; }

    /// <summary>
    /// Gets the model selection for the mode when configured.
    /// </summary>
    public AppModeModelInfo? Model { get; init; }

    /// <summary>
    /// Gets the prompt associated with the mode when configured.
    /// </summary>
    public string? Prompt { get; init; }

    /// <summary>
    /// Gets the temperature configured for the mode when available.
    /// </summary>
    public double? Temperature { get; init; }
}

/// <summary>
/// Describes the model binding for an application mode.
/// </summary>
public sealed class AppModeModelInfo
{
    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    [JsonPropertyName("modelID")]
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    [JsonPropertyName("providerID")]
    public required string ProviderId { get; init; }
}