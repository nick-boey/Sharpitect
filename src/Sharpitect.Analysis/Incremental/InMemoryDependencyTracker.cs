namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// In-memory implementation of dependency tracking.
/// </summary>
public sealed class InMemoryDependencyTracker : IDependencyTracker
{
    // Maps nodeId -> set of files that reference it
    private readonly Dictionary<string, HashSet<string>> _nodeToFiles = new();

    // Maps filePath -> set of nodes it references
    private readonly Dictionary<string, HashSet<string>> _fileToNodes = new();

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>();

    /// <inheritdoc />
    public void RecordReference(string sourceFilePath, string targetNodeId)
    {
        // Add to nodeId -> files mapping
        if (!_nodeToFiles.TryGetValue(targetNodeId, out var files))
        {
            files = new HashSet<string>();
            _nodeToFiles[targetNodeId] = files;
        }
        files.Add(sourceFilePath);

        // Add to file -> nodeIds mapping
        if (!_fileToNodes.TryGetValue(sourceFilePath, out var nodes))
        {
            nodes = new HashSet<string>();
            _fileToNodes[sourceFilePath] = nodes;
        }
        nodes.Add(targetNodeId);
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetDependentFiles(string nodeId)
    {
        return _nodeToFiles.TryGetValue(nodeId, out var files) ? files : EmptySet;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetDependentFilesForNodes(IEnumerable<string> nodeIds)
    {
        var result = new HashSet<string>();
        foreach (var nodeId in nodeIds)
        {
            if (_nodeToFiles.TryGetValue(nodeId, out var files))
            {
                result.UnionWith(files);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetReferencedNodes(string filePath)
    {
        return _fileToNodes.TryGetValue(filePath, out var nodes) ? nodes : EmptySet;
    }

    /// <inheritdoc />
    public void RemoveReferencesFromFile(string filePath)
    {
        if (!_fileToNodes.TryGetValue(filePath, out var referencedNodes))
        {
            return;
        }

        // Remove this file from all nodes it references
        foreach (var nodeId in referencedNodes)
        {
            if (_nodeToFiles.TryGetValue(nodeId, out var files))
            {
                files.Remove(filePath);
                // Clean up empty sets
                if (files.Count == 0)
                {
                    _nodeToFiles.Remove(nodeId);
                }
            }
        }

        // Clear the file's reference list
        _fileToNodes.Remove(filePath);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _nodeToFiles.Clear();
        _fileToNodes.Clear();
    }
}
