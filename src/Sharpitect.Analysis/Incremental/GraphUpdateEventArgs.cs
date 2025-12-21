namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Event args for graph update completion.
/// </summary>
public sealed class GraphUpdateEventArgs : EventArgs
{
    /// <summary>
    /// Gets the files that were updated.
    /// </summary>
    public required IReadOnlyList<string> UpdatedFiles { get; init; }

    /// <summary>
    /// Gets the number of nodes added.
    /// </summary>
    public required int NodesAdded { get; init; }

    /// <summary>
    /// Gets the number of nodes removed.
    /// </summary>
    public required int NodesRemoved { get; init; }

    /// <summary>
    /// Gets the number of edges added.
    /// </summary>
    public required int EdgesAdded { get; init; }

    /// <summary>
    /// Gets the number of edges removed.
    /// </summary>
    public required int EdgesRemoved { get; init; }

    /// <summary>
    /// Gets the duration of the update operation.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}

/// <summary>
/// Event args for graph update errors.
/// </summary>
public sealed class GraphUpdateErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the file that caused the error.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public required Exception Exception { get; init; }
}
