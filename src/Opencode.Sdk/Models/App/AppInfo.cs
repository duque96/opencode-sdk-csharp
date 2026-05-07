namespace Opencode.Models.App;

/// <summary>
/// Describes the current app environment returned by the API.
/// </summary>
public sealed class AppInfo
{
    /// <summary>
    /// Gets a value indicating whether the current working directory is a Git repository.
    /// </summary>
    public bool Git { get; init; }

    /// <summary>
    /// Gets the current machine hostname.
    /// </summary>
    public required string Hostname { get; init; }

    /// <summary>
    /// Gets the resolved application paths.
    /// </summary>
    public required AppPathInfo Path { get; init; }

    /// <summary>
    /// Gets application timing metadata.
    /// </summary>
    public AppTimeInfo Time { get; init; } = new();
}

/// <summary>
/// Describes the important paths returned by the API.
/// </summary>
public sealed class AppPathInfo
{
    /// <summary>
    /// Gets the config directory path.
    /// </summary>
    public required string Config { get; init; }

    /// <summary>
    /// Gets the current working directory path.
    /// </summary>
    public required string Cwd { get; init; }

    /// <summary>
    /// Gets the data directory path.
    /// </summary>
    public required string Data { get; init; }

    /// <summary>
    /// Gets the root directory path.
    /// </summary>
    public required string Root { get; init; }

    /// <summary>
    /// Gets the state directory path.
    /// </summary>
    public required string State { get; init; }
}

/// <summary>
/// Describes application timing metadata returned by the API.
/// </summary>
public sealed class AppTimeInfo
{
    /// <summary>
    /// Gets the initialization timestamp when available.
    /// </summary>
    public long? Initialized { get; init; }
}