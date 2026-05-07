namespace Opencode.Models.File;

/// <summary>
/// Describes the content returned when reading a workspace file.
/// </summary>
public sealed class FileReadResponse
{
    /// <summary>
    /// Gets the raw file content or patch text.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the representation type for <see cref="Content"/>.
    /// </summary>
    public required string Type { get; init; }
}