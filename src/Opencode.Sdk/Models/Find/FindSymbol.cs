namespace Opencode.Models.Find;

/// <summary>
/// Describes a workspace symbol returned by a find operation.
/// </summary>
public sealed class FindSymbol
{
    /// <summary>
    /// Gets the symbol kind identifier returned by the API.
    /// </summary>
    public int Kind { get; init; }

    /// <summary>
    /// Gets the source location for the symbol.
    /// </summary>
    public required FindSymbolLocation Location { get; init; }

    /// <summary>
    /// Gets the symbol display name.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Describes the source location of a workspace symbol.
/// </summary>
public sealed class FindSymbolLocation
{
    /// <summary>
    /// Gets the symbol range inside the source file.
    /// </summary>
    public required FindRange Range { get; init; }

    /// <summary>
    /// Gets the URI of the source file containing the symbol.
    /// </summary>
    public required string Uri { get; init; }
}

/// <summary>
/// Describes a source range returned by a find operation.
/// </summary>
public sealed class FindRange
{
    /// <summary>
    /// Gets the end position of the range.
    /// </summary>
    public required FindPosition End { get; init; }

    /// <summary>
    /// Gets the start position of the range.
    /// </summary>
    public required FindPosition Start { get; init; }
}

/// <summary>
/// Describes a line and character position returned by a find operation.
/// </summary>
public sealed class FindPosition
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