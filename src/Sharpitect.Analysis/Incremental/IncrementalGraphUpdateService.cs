using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Service that manages incremental updates to the declaration graph.
/// </summary>
public sealed class IncrementalGraphUpdateService : IIncrementalGraphUpdateService
{
    private readonly Workspace _workspace;
    private readonly IGraphRepository _repository;
    private readonly DeclarationGraph _graph;
    private readonly IDependencyTracker _dependencyTracker;
    private readonly IIncrementalFileAnalyzer _fileAnalyzer;
    private readonly IFileChangeWatcher? _fileWatcher;
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private readonly Dictionary<string, string> _symbolMappings = new();
    private readonly string _solutionRootDirectory;

    private IncrementalUpdateState _state = IncrementalUpdateState.Stopped;
    private bool _visitLocals;

    /// <summary>
    /// Creates a new incremental graph update service.
    /// </summary>
    /// <param name="workspace">The Roslyn workspace.</param>
    /// <param name="repository">The graph repository for persistence.</param>
    /// <param name="graph">The in-memory declaration graph.</param>
    /// <param name="dependencyTracker">The dependency tracker.</param>
    /// <param name="fileAnalyzer">The file analyzer.</param>
    /// <param name="fileWatcher">Optional file watcher for monitoring changes.</param>
    /// <param name="visitLocals">Whether to include local variables.</param>
    /// <param name="solutionRootDirectory">Optional solution root directory. If not provided, will be inferred from the workspace's solution path.</param>
    public IncrementalGraphUpdateService(
        Workspace workspace,
        IGraphRepository repository,
        DeclarationGraph graph,
        IDependencyTracker dependencyTracker,
        IIncrementalFileAnalyzer fileAnalyzer,
        IFileChangeWatcher? fileWatcher = null,
        bool visitLocals = false,
        string? solutionRootDirectory = null)
    {
        _workspace = workspace;
        _repository = repository;
        _graph = graph;
        _dependencyTracker = dependencyTracker;
        _fileAnalyzer = fileAnalyzer;
        _fileWatcher = fileWatcher;
        _visitLocals = visitLocals;
        _solutionRootDirectory = solutionRootDirectory
            ?? PathHelper.GetSolutionRootDirectory(
                workspace.CurrentSolution.FilePath ?? throw new InvalidOperationException("Solution path is required"));

        if (_fileWatcher != null)
        {
            _fileWatcher.ChangesDetected += OnFileChangesDetected;
        }
    }

    /// <inheritdoc />
    public event EventHandler<GraphUpdateEventArgs>? UpdateCompleted;

    /// <inheritdoc />
    public event EventHandler<GraphUpdateErrorEventArgs>? UpdateError;

    /// <inheritdoc />
    public IncrementalUpdateState State => _state;

    /// <inheritdoc />
    public DeclarationGraph Graph => _graph;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_state != IncrementalUpdateState.Stopped)
        {
            return;
        }

        if (_fileWatcher != null)
        {
            var solutionPath = _workspace.CurrentSolution.FilePath;
            if (solutionPath != null)
            {
                var directory = Path.GetDirectoryName(solutionPath);
                if (directory != null)
                {
                    await _fileWatcher.StartAsync(directory, cancellationToken);
                }
            }
        }

        _state = IncrementalUpdateState.Watching;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_state == IncrementalUpdateState.Stopped)
        {
            return;
        }

        if (_fileWatcher != null)
        {
            await _fileWatcher.StopAsync(cancellationToken);
        }

        _state = IncrementalUpdateState.Stopped;
    }

    /// <inheritdoc />
    public async Task UpdateFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        var changes = filePaths.Select(fp => new FileChange(fp, FileChangeKind.Modified)).ToList();
        await ProcessFileChangesAsync(changes, enableCascade: true, cancellationToken);
    }

    /// <summary>
    /// Processes file changes and updates the graph.
    /// </summary>
    public async Task ProcessFileChangesAsync(
        IReadOnlyList<FileChange> changes,
        bool enableCascade = true,
        CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0)
        {
            return;
        }

        await _updateLock.WaitAsync(cancellationToken);
        try
        {
            _state = IncrementalUpdateState.Updating;
            var stopwatch = Stopwatch.StartNew();

            var processedFiles = new HashSet<string>();
            var totalNodesAdded = 0;
            var totalNodesRemoved = 0;
            var totalEdgesAdded = 0;
            var totalEdgesRemoved = 0;

            // Convert incoming file paths to relative paths and queue for processing
            var filesToProcess = new Queue<FileChange>(
                changes.Select(c => new FileChange(
                    PathHelper.ToRelativePath(c.FilePath, _solutionRootDirectory),
                    c.Kind)));
            var filesProcessedThisRound = new HashSet<string>();

            while (filesToProcess.Count > 0)
            {
                var change = filesToProcess.Dequeue();

                // Skip if already processed in this update cycle
                if (!filesProcessedThisRound.Add(change.FilePath))
                {
                    continue;
                }

                try
                {
                    var (nodesRemoved, edgesRemoved, affectedNodeIds) = await RemoveFileDataAsync(
                        change.FilePath, cancellationToken);

                    totalNodesRemoved += nodesRemoved;
                    totalEdgesRemoved += edgesRemoved;

                    // If file was deleted, we're done with it
                    if (change.Kind == FileChangeKind.Deleted)
                    {
                        processedFiles.Add(change.FilePath);

                        // Queue dependent files for re-analysis (eager cascade)
                        if (enableCascade)
                        {
                            var dependentFiles = _dependencyTracker.GetDependentFilesForNodes(affectedNodeIds);
                            foreach (var depFile in dependentFiles)
                            {
                                if (!filesProcessedThisRound.Contains(depFile))
                                {
                                    filesToProcess.Enqueue(new FileChange(depFile, FileChangeKind.Modified));
                                }
                            }
                        }

                        continue;
                    }

                    // Re-analyze the file
                    var (nodesAdded, edgesAdded) = await AnalyzeFileAsync(
                        change.FilePath, cancellationToken);

                    totalNodesAdded += nodesAdded;
                    totalEdgesAdded += edgesAdded;
                    processedFiles.Add(change.FilePath);

                    // Queue dependent files if symbols changed (eager cascade)
                    if (enableCascade && affectedNodeIds.Count > 0)
                    {
                        var dependentFiles = _dependencyTracker.GetDependentFilesForNodes(affectedNodeIds);
                        foreach (var depFile in dependentFiles)
                        {
                            if (!filesProcessedThisRound.Contains(depFile))
                            {
                                filesToProcess.Enqueue(new FileChange(depFile, FileChangeKind.Modified));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateError?.Invoke(this, new GraphUpdateErrorEventArgs
                    {
                        FilePath = change.FilePath,
                        Exception = ex
                    });
                }
            }

            stopwatch.Stop();

            if (processedFiles.Count > 0)
            {
                UpdateCompleted?.Invoke(this, new GraphUpdateEventArgs
                {
                    UpdatedFiles = processedFiles.ToList(),
                    NodesAdded = totalNodesAdded,
                    NodesRemoved = totalNodesRemoved,
                    EdgesAdded = totalEdgesAdded,
                    EdgesRemoved = totalEdgesRemoved,
                    Duration = stopwatch.Elapsed
                });
            }
        }
        finally
        {
            _state = _fileWatcher?.IsWatching == true
                ? IncrementalUpdateState.Watching
                : IncrementalUpdateState.Stopped;
            _updateLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _updateLock.Dispose();

        if (_fileWatcher != null)
        {
            _fileWatcher.ChangesDetected -= OnFileChangesDetected;
        }
    }

    private void OnFileChangesDetected(object? sender, IReadOnlyList<FileChange> changes)
    {
        // Fire and forget - errors will be reported via UpdateError event
        _ = ProcessFileChangesAsync(changes, enableCascade: true);
    }

    private async Task<(int nodesRemoved, int edgesRemoved, List<string> affectedNodeIds)> RemoveFileDataAsync(
        string filePath, CancellationToken cancellationToken)
    {
        // Get nodes in this file
        var nodesInFile = _graph.GetNodesByFile(filePath).ToList();
        var nodeIds = nodesInFile.Select(n => n.Id).ToList();

        // Get edges from this file
        var edgesFromFile = _graph.GetEdgesBySourceFile(filePath).ToList();

        // Remove from dependency tracker
        _dependencyTracker.RemoveReferencesFromFile(filePath);

        // Remove from in-memory graph
        _graph.RemoveNodesByFile(filePath);
        _graph.RemoveEdgesBySourceFile(filePath);

        // Remove dangling edges that pointed to removed nodes
        foreach (var nodeId in nodeIds)
        {
            _graph.RemoveEdgesByNodeId(nodeId);
        }

        // Remove from repository
        await _repository.DeleteNodesByFileAsync(filePath, cancellationToken);
        await _repository.DeleteEdgesBySourceFileAsync(filePath, cancellationToken);

        // Remove from symbol mappings
        foreach (var node in nodesInFile)
        {
            _symbolMappings.Remove(node.Id);
        }

        return (nodesInFile.Count, edgesFromFile.Count, nodeIds);
    }

    private async Task<(int nodesAdded, int edgesAdded)> AnalyzeFileAsync(
        string relativePath, CancellationToken cancellationToken)
    {
        // Find the document in the workspace by comparing relative paths
        var document = _workspace.CurrentSolution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => d.FilePath != null &&
                PathHelper.ToRelativePath(d.FilePath, _solutionRootDirectory) == relativePath);

        if (document == null)
        {
            return (0, 0);
        }

        var compilation = await document.Project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            return (0, 0);
        }

        // Get existing node IDs for reference resolution
        var existingNodeIds = new HashSet<string>(_graph.Nodes.Keys);

        // Analyze the file
        var result = await _fileAnalyzer.AnalyzeFileAsync(
            document,
            compilation,
            _symbolMappings,
            existingNodeIds,
            _solutionRootDirectory,
            _visitLocals,
            cancellationToken);

        // Add to in-memory graph
        _graph.AddNodes(result.Nodes);
        _graph.AddEdges(result.Edges);

        // Update symbol mappings
        foreach (var kvp in result.SymbolMappings)
        {
            _symbolMappings[kvp.Key] = kvp.Value;
        }

        // Update dependency tracker
        foreach (var edge in result.Edges)
        {
            if (edge.SourceFilePath != null && edge.Kind != RelationshipKind.Contains)
            {
                _dependencyTracker.RecordReference(edge.SourceFilePath, edge.TargetId);
            }
        }

        // Persist to repository
        await _repository.SaveNodesAsync(result.Nodes, cancellationToken);
        await _repository.SaveEdgesAsync(result.Edges, cancellationToken);

        return (result.Nodes.Count, result.Edges.Count);
    }
}
