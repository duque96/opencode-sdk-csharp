using System.Text.Json.Serialization;

namespace Opencode.Models.App;

/// <summary>
/// Describes the provider metadata response returned by the app resource.
/// </summary>
public sealed class AppProvidersInfo
{
    /// <summary>
    /// Gets the default provider selections keyed by capability.
    /// </summary>
    [JsonPropertyName("default")]
    public required Dictionary<string, string> Defaults { get; init; }

    /// <summary>
    /// Gets the available model providers.
    /// </summary>
    public required AppProviderInfo[] Providers { get; init; }
}

/// <summary>
/// Describes one model provider returned by the API.
/// </summary>
public sealed class AppProviderInfo
{
    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the environment variables used by the provider.
    /// </summary>
    public required string[] Env { get; init; }

    /// <summary>
    /// Gets the models exposed by the provider keyed by model identifier.
    /// </summary>
    public required Dictionary<string, AppModelInfo> Models { get; init; }

    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider API endpoint when available.
    /// </summary>
    public string? Api { get; init; }

    /// <summary>
    /// Gets the related npm package name when available.
    /// </summary>
    public string? Npm { get; init; }
}