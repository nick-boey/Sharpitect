namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Tracks dependencies between files and nodes for incremental updates.
/// Used to determine which files need re-analysis when a symbol changes.
/// </summary>
public interface IDependencyTracker
{
    /// <summary>
    /// Records that a file references a node (symbol).
    /// </summary>
    /// <param name="sourceFilePath">The file that contains the reference.</param>
    /// <param name="targetNodeId">The node ID being referenced.</param>
    void RecordReference(string sourceFilePath, string targetNodeId);

    /// <summary>
    /// Gets all files that reference a specific node.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>Set of file paths that reference the node.</returns>
    IReadOnlySet<string> GetDependentFiles(string nodeId);

    /// <summary>
    /// Gets all files that reference any of the specified nodes.
    /// </summary>
    /// <param name="nodeIds">The node IDs.</param>
    /// <returns>Set of file paths that reference any of the nodes.</returns>
    IReadOnlySet<string> GetDependentFilesForNodes(IEnumerable<string> nodeIds);

    /// <summary>
    /// Gets all nodes referenced by a specific file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>Set of node IDs referenced by the file.</returns>
    IReadOnlySet<string> GetReferencedNodes(string filePath);

    /// <summary>
    /// Removes all reference records from a file.
    /// Called when a file is about to be re-analyzed.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    void RemoveReferencesFromFile(string filePath);

    /// <summary>
    /// Clears all dependency tracking data.
    /// </summary>
    void Clear();
}
