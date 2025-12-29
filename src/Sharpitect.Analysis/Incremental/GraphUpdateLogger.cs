using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Logger for graph update operations that writes to console and optionally to a file.
/// </summary>
public sealed class GraphUpdateLogger : IDisposable
{
    private readonly StreamWriter? _fileWriter;
    private readonly object _lock = new();
    private readonly bool _enableConsole;
    private bool _disposed;

    /// <summary>
    /// Creates a new graph update logger.
    /// </summary>
    /// <param name="logFilePath">Optional path to log file. If null, only console logging is enabled.</param>
    /// <param name="enableConsole">Whether to enable console logging. Default is true.</param>
    public GraphUpdateLogger(string? logFilePath = null, bool enableConsole = true)
    {
        _enableConsole = enableConsole;

        if (logFilePath != null)
        {
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _fileWriter = new StreamWriter(logFilePath, append: true)
            {
                AutoFlush = true
            };
        }
    }

    /// <summary>
    /// Logs a file change detection event.
    /// </summary>
    public void LogFileChangeDetected(FileChange change)
    {
        var message = $"[FILE CHANGE] {change.Kind}: {change.FilePath}";
        if (change.OldFilePath != null)
        {
            message += $" (from: {change.OldFilePath})";
        }
        Log("FILE_CHANGE", message);
    }

    /// <summary>
    /// Logs when a batch of file changes is about to be processed.
    /// </summary>
    public void LogBatchProcessingStarted(IReadOnlyList<FileChange> changes)
    {
        Log("BATCH_START", $"Processing batch of {changes.Count} file change(s)");
        foreach (var change in changes)
        {
            Log("BATCH_START", $"  - {change.Kind}: {change.FilePath}");
        }
    }

    /// <summary>
    /// Logs when file processing begins.
    /// </summary>
    public void LogFileProcessingStarted(string filePath, FileChangeKind kind)
    {
        Log("FILE_PROCESS", $"Started processing {kind} for: {filePath}");
    }

    /// <summary>
    /// Logs nodes that will be removed from a file.
    /// </summary>
    public void LogNodesRemoved(string filePath, IReadOnlyList<DeclarationNode> nodes)
    {
        if (nodes.Count == 0)
        {
            Log("NODES_REMOVED", $"No nodes to remove from: {filePath}");
            return;
        }

        Log("NODES_REMOVED", $"Removing {nodes.Count} node(s) from: {filePath}");
        foreach (var node in nodes)
        {
            Log("NODES_REMOVED", $"  - [{node.Kind}] {node.Id} (lines {node.StartLine}-{node.EndLine})");
        }
    }

    /// <summary>
    /// Logs edges that will be removed from a file.
    /// </summary>
    public void LogEdgesRemoved(string filePath, IReadOnlyList<RelationshipEdge> edges)
    {
        if (edges.Count == 0)
        {
            Log("EDGES_REMOVED", $"No edges to remove from: {filePath}");
            return;
        }

        Log("EDGES_REMOVED", $"Removing {edges.Count} edge(s) from: {filePath}");
        foreach (var edge in edges)
        {
            Log("EDGES_REMOVED", $"  - [{edge.Kind}] {edge.SourceId} -> {edge.TargetId}");
        }
    }

    /// <summary>
    /// Logs dangling edges removed for a node.
    /// </summary>
    public void LogDanglingEdgesRemoved(string nodeId, int count)
    {
        if (count > 0)
        {
            Log("DANGLING_EDGES", $"Removed {count} dangling edge(s) referencing node: {nodeId}");
        }
    }

    /// <summary>
    /// Logs nodes that were added from file analysis.
    /// </summary>
    public void LogNodesAdded(string filePath, IReadOnlyList<DeclarationNode> nodes)
    {
        if (nodes.Count == 0)
        {
            Log("NODES_ADDED", $"No nodes added from: {filePath}");
            return;
        }

        Log("NODES_ADDED", $"Adding {nodes.Count} node(s) from: {filePath}");
        foreach (var node in nodes)
        {
            Log("NODES_ADDED", $"  + [{node.Kind}] {node.Id} (lines {node.StartLine}-{node.EndLine})");
        }
    }

    /// <summary>
    /// Logs edges that were added from file analysis.
    /// </summary>
    public void LogEdgesAdded(string filePath, IReadOnlyList<RelationshipEdge> edges)
    {
        if (edges.Count == 0)
        {
            Log("EDGES_ADDED", $"No edges added from: {filePath}");
            return;
        }

        Log("EDGES_ADDED", $"Adding {edges.Count} edge(s) from: {filePath}");
        foreach (var edge in edges)
        {
            Log("EDGES_ADDED", $"  + [{edge.Kind}] {edge.SourceId} -> {edge.TargetId}");
        }
    }

    /// <summary>
    /// Logs dependent files that will be re-analyzed due to cascade.
    /// </summary>
    public void LogCascadeTriggered(string sourceFile, IReadOnlyList<string> affectedNodeIds, IReadOnlyList<string> dependentFiles)
    {
        if (dependentFiles.Count == 0)
        {
            return;
        }

        Log("CASCADE", $"Change in {sourceFile} affects {affectedNodeIds.Count} node(s), triggering re-analysis of {dependentFiles.Count} dependent file(s):");
        foreach (var depFile in dependentFiles)
        {
            Log("CASCADE", $"  -> {depFile}");
        }
    }

    /// <summary>
    /// Logs completion of a batch update.
    /// </summary>
    public void LogBatchCompleted(GraphUpdateEventArgs args)
    {
        Log("BATCH_COMPLETE", $"Batch completed in {args.Duration.TotalMilliseconds:F0}ms");
        Log("BATCH_COMPLETE", $"  Files processed: {args.UpdatedFiles.Count}");
        Log("BATCH_COMPLETE", $"  Nodes added: {args.NodesAdded}, removed: {args.NodesRemoved}");
        Log("BATCH_COMPLETE", $"  Edges added: {args.EdgesAdded}, removed: {args.EdgesRemoved}");
        Log("BATCH_COMPLETE", "-----------------------------------------------------------");
    }

    /// <summary>
    /// Logs an error during file processing.
    /// </summary>
    public void LogError(string filePath, Exception exception)
    {
        Log("ERROR", $"Error processing {filePath}: {exception.Message}");
        Log("ERROR", $"  Stack trace: {exception.StackTrace}");
    }

    /// <summary>
    /// Logs debounce timer started.
    /// </summary>
    public void LogDebounceStarted(string filePath, TimeSpan interval)
    {
        Log("DEBOUNCE", $"Debounce timer started for {filePath} ({interval.TotalMilliseconds}ms)");
    }

    /// <summary>
    /// Logs debounce timer elapsed.
    /// </summary>
    public void LogDebounceElapsed(int pendingChangeCount)
    {
        Log("DEBOUNCE", $"Debounce elapsed, emitting {pendingChangeCount} change(s)");
    }

    private void Log(string category, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var formattedMessage = $"[{timestamp}] [{category}] {message}";

        lock (_lock)
        {
            if (_enableConsole)
            {
                Console.Error.WriteLine(formattedMessage);
            }

            _fileWriter?.WriteLine(formattedMessage);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _fileWriter?.Dispose();
        _disposed = true;
    }
}
