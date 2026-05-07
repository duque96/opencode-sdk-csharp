using System.Text.Json;
using System.Text.Json.Serialization;

namespace Opencode.Models.Config;

/// <summary>
/// Describes the configuration document returned by the API.
/// </summary>
public sealed class ConfigInfo
{
    /// <summary>
    /// Gets the JSON schema reference for configuration validation when present.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// Gets the configured agents keyed by agent name.
    /// </summary>
    public Dictionary<string, ConfigAgentDefinition>? Agent { get; init; }

    /// <summary>
    /// Gets a value indicating whether sessions are shared automatically.
    /// </summary>
    public bool? Autoshare { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic updates are enabled.
    /// </summary>
    public bool? Autoupdate { get; init; }

    /// <summary>
    /// Gets the provider identifiers disabled from automatic loading.
    /// </summary>
    [JsonPropertyName("disabled_providers")]
    public string[]? DisabledProviders { get; init; }

    /// <summary>
    /// Gets experimental configuration sections when present.
    /// </summary>
    public ConfigExperimentalInfo? Experimental { get; init; }

    /// <summary>
    /// Gets additional instruction file paths or patterns.
    /// </summary>
    public string[]? Instructions { get; init; }

    /// <summary>
    /// Gets custom key bindings keyed by action name.
    /// </summary>
    public Dictionary<string, string>? Keybinds { get; init; }

    /// <summary>
    /// Gets the configured layout name when present.
    /// </summary>
    public string? Layout { get; init; }

    /// <summary>
    /// Gets the configured MCP servers keyed by server name.
    /// </summary>
    public Dictionary<string, ConfigMcpServerInfo>? Mcp { get; init; }

    /// <summary>
    /// Gets the configured modes keyed by mode name.
    /// </summary>
    public Dictionary<string, ConfigModeDefinition>? Mode { get; init; }

    /// <summary>
    /// Gets the default model identifier.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the configured providers keyed by provider name.
    /// </summary>
    public Dictionary<string, ConfigProviderInfo>? Provider { get; init; }

    /// <summary>
    /// Gets the session sharing mode.
    /// </summary>
    public string? Share { get; init; }

    /// <summary>
    /// Gets the small-model identifier used for lightweight tasks.
    /// </summary>
    [JsonPropertyName("small_model")]
    public string? SmallModel { get; init; }

    /// <summary>
    /// Gets the configured theme name.
    /// </summary>
    public string? Theme { get; init; }

    /// <summary>
    /// Gets the configured display username.
    /// </summary>
    public string? Username { get; init; }
}

/// <summary>
/// Describes one configured agent entry.
/// </summary>
public sealed class ConfigAgentDefinition
{
    /// <summary>
    /// Gets a value indicating whether the agent is disabled.
    /// </summary>
    public bool? Disable { get; init; }

    /// <summary>
    /// Gets the description of the agent.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the configured model identifier.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the configured prompt.
    /// </summary>
    public string? Prompt { get; init; }

    /// <summary>
    /// Gets the configured temperature.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Gets the tool availability map for the agent.
    /// </summary>
    public Dictionary<string, bool>? Tools { get; init; }
}

/// <summary>
/// Describes one configured mode entry.
/// </summary>
public sealed class ConfigModeDefinition
{
    /// <summary>
    /// Gets a value indicating whether the mode is disabled.
    /// </summary>
    public bool? Disable { get; init; }

    /// <summary>
    /// Gets the configured model identifier.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the configured prompt.
    /// </summary>
    public string? Prompt { get; init; }

    /// <summary>
    /// Gets the configured temperature.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// Gets the tool availability map for the mode.
    /// </summary>
    public Dictionary<string, bool>? Tools { get; init; }
}

/// <summary>
/// Describes the experimental configuration section.
/// </summary>
public sealed class ConfigExperimentalInfo
{
    /// <summary>
    /// Gets the configured hooks when present.
    /// </summary>
    public ConfigHookInfo? Hook { get; init; }
}

/// <summary>
/// Describes the experimental hook configuration.
/// </summary>
public sealed class ConfigHookInfo
{
    /// <summary>
    /// Gets hooks that run after file edits keyed by hook group.
    /// </summary>
    [JsonPropertyName("file_edited")]
    public Dictionary<string, ConfigHookCommand[]>? FileEdited { get; init; }

    /// <summary>
    /// Gets hooks that run after session completion.
    /// </summary>
    [JsonPropertyName("session_completed")]
    public ConfigHookCommand[]? SessionCompleted { get; init; }
}

/// <summary>
/// Describes one external command hook.
/// </summary>
public sealed class ConfigHookCommand
{
    /// <summary>
    /// Gets the command and arguments to execute.
    /// </summary>
    public required string[] Command { get; init; }

    /// <summary>
    /// Gets environment variables passed to the command when present.
    /// </summary>
    public Dictionary<string, string>? Environment { get; init; }
}

/// <summary>
/// Describes one MCP server configuration entry.
/// </summary>
public sealed class ConfigMcpServerInfo
{
    /// <summary>
    /// Gets the MCP connection type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the command and arguments for local MCP servers.
    /// </summary>
    public string[]? Command { get; init; }

    /// <summary>
    /// Gets a value indicating whether the server is enabled on startup.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets environment variables for local MCP servers.
    /// </summary>
    public Dictionary<string, string>? Environment { get; init; }

    /// <summary>
    /// Gets request headers for remote MCP servers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets the remote MCP server URL when applicable.
    /// </summary>
    public string? Url { get; init; }
}

/// <summary>
/// Describes one configured provider entry.
/// </summary>
public sealed class ConfigProviderInfo
{
    /// <summary>
    /// Gets the configured models keyed by model name.
    /// </summary>
    public required Dictionary<string, ConfigProviderModelInfo> Models { get; init; }

    /// <summary>
    /// Gets the provider identifier when overridden.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the provider API endpoint when overridden.
    /// </summary>
    public string? Api { get; init; }

    /// <summary>
    /// Gets environment variables used by the provider when present.
    /// </summary>
    public string[]? Env { get; init; }

    /// <summary>
    /// Gets the display name when overridden.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the npm package name when present.
    /// </summary>
    public string? Npm { get; init; }

    /// <summary>
    /// Gets provider-specific options when present.
    /// </summary>
    public Dictionary<string, JsonElement>? Options { get; init; }
}

/// <summary>
/// Describes one model override inside a provider configuration.
/// </summary>
public sealed class ConfigProviderModelInfo
{
    /// <summary>
    /// Gets the model identifier when overridden.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports attachments.
    /// </summary>
    public bool? Attachment { get; init; }

    /// <summary>
    /// Gets the cost metadata when present.
    /// </summary>
    public ConfigProviderModelCostInfo? Cost { get; init; }

    /// <summary>
    /// Gets the limit metadata when present.
    /// </summary>
    public ConfigProviderModelLimitInfo? Limit { get; init; }

    /// <summary>
    /// Gets the display name when overridden.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets provider-specific model options when present.
    /// </summary>
    public Dictionary<string, JsonElement>? Options { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports reasoning.
    /// </summary>
    public bool? Reasoning { get; init; }

    /// <summary>
    /// Gets the release date string when present.
    /// </summary>
    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports configurable temperature.
    /// </summary>
    public bool? Temperature { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model supports tool calls.
    /// </summary>
    [JsonPropertyName("tool_call")]
    public bool? ToolCall { get; init; }
}

/// <summary>
/// Describes cost metadata for a configured provider model.
/// </summary>
public sealed class ConfigProviderModelCostInfo
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
    /// Gets the cache read cost when present.
    /// </summary>
    [JsonPropertyName("cache_read")]
    public decimal? CacheRead { get; init; }

    /// <summary>
    /// Gets the cache write cost when present.
    /// </summary>
    [JsonPropertyName("cache_write")]
    public decimal? CacheWrite { get; init; }
}

/// <summary>
/// Describes limit metadata for a configured provider model.
/// </summary>
public sealed class ConfigProviderModelLimitInfo
{
    /// <summary>
    /// Gets the context window size.
    /// </summary>
    public int Context { get; init; }

    /// <summary>
    /// Gets the output token limit.
    /// </summary>
    public int Output { get; init; }
}