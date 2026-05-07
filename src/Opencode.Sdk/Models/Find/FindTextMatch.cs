using System.Text.Json.Serialization;

namespace Opencode.Models.Find;

/// <summary>
/// Describes one file-level text match returned by a find operation.
/// </summary>
public sealed class FindTextMatch
{
    /// <summary>
    /// Gets the absolute byte or character offset reported by the API.
    /// </summary>
    [JsonPropertyName("absolute_offset")]
    public int AbsoluteOffset { get; init; }

    /// <summary>
    /// Gets the one-based line number containing the match.
    /// </summary>
    [JsonPropertyName("line_number")]
    public int LineNumber { get; init; }

    /// <summary>
    /// Gets the line text metadata for the match.
    /// </summary>
    public required FindTextValue Lines { get; init; }

    /// <summary>
    /// Gets the path metadata for the match.
    /// </summary>
    public required FindTextValue Path { get; init; }

    /// <summary>
    /// Gets the submatches reported within the line.
    /// </summary>
    public required FindTextSubmatch[] Submatches { get; init; }
}

/// <summary>
/// Wraps a textual value returned by the API for a find response.
/// </summary>
public sealed class FindTextValue
{
    /// <summary>
    /// Gets the wrapped text value.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Describes a single submatch inside a text search line.
/// </summary>
public sealed class FindTextSubmatch
{
    /// <summary>
    /// Gets the zero-based exclusive end offset within the line.
    /// </summary>
    public int End { get; init; }

    /// <summary>
    /// Gets the matched text metadata.
    /// </summary>
    public required FindTextValue Match { get; init; }

    /// <summary>
    /// Gets the zero-based inclusive start offset within the line.
    /// </summary>
    public int Start { get; init; }
}