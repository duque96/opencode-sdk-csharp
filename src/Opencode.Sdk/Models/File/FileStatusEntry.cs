namespace Opencode.Models.File;

/// <summary>
/// Describes the current status of a workspace file.
/// </summary>
public sealed class FileStatusEntry
{
    /// <summary>
    /// Gets the number of added lines.
    /// </summary>
    public int Added { get; init; }

    /// <summary>
    /// Gets the workspace-relative file path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the number of removed lines.
    /// </summary>
    public int Removed { get; init; }

    /// <summary>
    /// Gets the file change status reported by the API.
    /// </summary>
    public required string Status { get; init; }
}