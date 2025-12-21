using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Incremental;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Orchestrates solution-level analysis and graph construction.
/// </summary>
public sealed class GraphSolutionAnalyzer
{
    private static bool _msBuildRegistered;
    private static readonly Lock RegistrationLock = new();

    private readonly IGraphRepository _repository;
    private readonly SemanticProjectAnalyzer _projectAnalyzer;

    /// <summary>
    /// Creates a new graph solution analyzer.
    /// </summary>
    /// <param name="repository">The repository for persisting the graph.</param>
    public GraphSolutionAnalyzer(IGraphRepository repository)
    {
        _repository = repository;
        _projectAnalyzer = new SemanticProjectAnalyzer();
    }

    /// <summary>
    /// Analyzes a solution and persists the declaration graph.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <param name="visitLocals">True to include local variables and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The declaration graph.</returns>
    public async Task<DeclarationGraph> AnalyzeAsync(string solutionPath, bool visitLocals = false,
        CancellationToken cancellationToken = default)
    {
        EnsureMSBuildRegistered();

        await _repository.InitializeAsync(cancellationToken);
        await _repository.ClearAsync(cancellationToken);

        var graph = new DeclarationGraph();

        using var workspace = MSBuildWorkspace.Create();

        // Subscribe to workspace diagnostics for debugging
        workspace.WorkspaceFailed += (_, e) =>
        {
            // Log or handle workspace failures if needed
            Console.Error.WriteLine($"Workspace warning: {e.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        // Create solution node
        var solutionNode = DeclarationNodeFactory.CreateSolutionNode(solutionPath);
        graph.AddNode(solutionNode);

        // Create project nodes and track them
        var projectNodes = new Dictionary<ProjectId, DeclarationNode>();
        foreach (var project in solution.Projects)
        {
            var projectNode = DeclarationNodeFactory.CreateProjectNode(project);
            graph.AddNode(projectNode);
            projectNodes[project.Id] = projectNode;

            // Add containment edge: solution contains project
            graph.AddEdge(new RelationshipEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = solutionNode.Id,
                TargetId = projectNode.Id,
                Kind = RelationshipKind.Contains
            });
        }

        // Create project dependency edges
        foreach (var project in solution.Projects)
        {
            var projectNode = projectNodes[project.Id];

            foreach (var projectRef in project.ProjectReferences)
            {
                if (projectNodes.TryGetValue(projectRef.ProjectId, out var referencedProjectNode))
                {
                    graph.AddEdge(new RelationshipEdge
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = projectNode.Id,
                        TargetId = referencedProjectNode.Id,
                        Kind = RelationshipKind.DependsOn
                    });
                }
            }
        }

        // Analyze each project
        var symbolToNodeId = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        var allNodeIds = new HashSet<string>(graph.Nodes.Keys);

        foreach (var project in solution.Projects)
        {
            var projectNode = projectNodes[project.Id];

            var result = await _projectAnalyzer.AnalyzeProjectAsync(
                project,
                symbolToNodeId,
                allNodeIds,
                visitLocals,
                cancellationToken);

            // Add project containment edges for namespaces (top-level namespaces in project)
            var topLevelNamespaces = result.Nodes
                .Where(n => n.Kind == DeclarationKind.Namespace)
                .GroupBy(n => GetTopLevelNamespace(n.Id))
                .Select(g => g.First())
                .ToList();

            foreach (var namespaceNode in topLevelNamespaces)
            {
                // Check if there's already a containment edge from this namespace to something else
                var hasParent = result.Edges.Any(e =>
                    e.Kind == RelationshipKind.Contains &&
                    e.TargetId == namespaceNode.Id);

                if (!hasParent)
                {
                    graph.AddEdge(new RelationshipEdge
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = projectNode.Id,
                        TargetId = namespaceNode.Id,
                        Kind = RelationshipKind.Contains
                    });
                }
            }

            graph.AddNodes(result.Nodes);
            graph.AddEdges(result.Edges);

            // Update mappings for next project
            foreach (var kvp in result.SymbolToNodeId)
            {
                symbolToNodeId[kvp.Key] = kvp.Value;
            }

            foreach (var node in result.Nodes)
            {
                allNodeIds.Add(node.Id);
            }
        }

        // Persist to repository
        await _repository.SaveNodesAsync(graph.Nodes.Values, cancellationToken);
        await _repository.SaveEdgesAsync(graph.Edges, cancellationToken);

        return graph;
    }

    /// <summary>
    /// Analyzes a solution and returns an update service that watches for file changes.
    /// The caller is responsible for disposing the returned service.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <param name="visitLocals">True to include local variables and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The incremental update service that manages the graph.</returns>
    public async Task<IIncrementalGraphUpdateService> WatchAsync(
        string solutionPath,
        bool visitLocals = false,
        CancellationToken cancellationToken = default)
    {
        EnsureMSBuildRegistered();

        await _repository.InitializeAsync(cancellationToken);
        await _repository.ClearAsync(cancellationToken);

        var graph = new DeclarationGraph();

        // Create workspace but DON'T dispose it - it will be used for incremental updates
        var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (_, e) =>
        {
            Console.Error.WriteLine($"Workspace warning: {e.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        // Create solution node
        var solutionNode = DeclarationNodeFactory.CreateSolutionNode(solutionPath);
        graph.AddNode(solutionNode);

        // Create project nodes and track them
        var projectNodes = new Dictionary<ProjectId, DeclarationNode>();
        foreach (var project in solution.Projects)
        {
            var projectNode = DeclarationNodeFactory.CreateProjectNode(project);
            graph.AddNode(projectNode);
            projectNodes[project.Id] = projectNode;

            graph.AddEdge(new RelationshipEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = solutionNode.Id,
                TargetId = projectNode.Id,
                Kind = RelationshipKind.Contains
            });
        }

        // Create project dependency edges
        foreach (var project in solution.Projects)
        {
            var projectNode = projectNodes[project.Id];

            foreach (var projectRef in project.ProjectReferences)
            {
                if (projectNodes.TryGetValue(projectRef.ProjectId, out var referencedProjectNode))
                {
                    graph.AddEdge(new RelationshipEdge
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = projectNode.Id,
                        TargetId = referencedProjectNode.Id,
                        Kind = RelationshipKind.DependsOn
                    });
                }
            }
        }

        // Analyze each project
        var symbolToNodeId = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        var allNodeIds = new HashSet<string>(graph.Nodes.Keys);
        var dependencyTracker = new InMemoryDependencyTracker();

        foreach (var project in solution.Projects)
        {
            var projectNode = projectNodes[project.Id];

            var result = await _projectAnalyzer.AnalyzeProjectAsync(
                project,
                symbolToNodeId,
                allNodeIds,
                visitLocals,
                cancellationToken);

            var topLevelNamespaces = result.Nodes
                .Where(n => n.Kind == DeclarationKind.Namespace)
                .GroupBy(n => GetTopLevelNamespace(n.Id))
                .Select(g => g.First())
                .ToList();

            foreach (var namespaceNode in topLevelNamespaces)
            {
                var hasParent = result.Edges.Any(e =>
                    e.Kind == RelationshipKind.Contains &&
                    e.TargetId == namespaceNode.Id);

                if (!hasParent)
                {
                    graph.AddEdge(new RelationshipEdge
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = projectNode.Id,
                        TargetId = namespaceNode.Id,
                        Kind = RelationshipKind.Contains
                    });
                }
            }

            graph.AddNodes(result.Nodes);
            graph.AddEdges(result.Edges);

            // Track dependencies for reference edges
            foreach (var edge in result.Edges)
            {
                if (edge.SourceFilePath != null && edge.Kind != RelationshipKind.Contains)
                {
                    dependencyTracker.RecordReference(edge.SourceFilePath, edge.TargetId);
                }
            }

            foreach (var kvp in result.SymbolToNodeId)
            {
                symbolToNodeId[kvp.Key] = kvp.Value;
            }

            foreach (var node in result.Nodes)
            {
                allNodeIds.Add(node.Id);
            }
        }

        // Persist to repository
        await _repository.SaveNodesAsync(graph.Nodes.Values, cancellationToken);
        await _repository.SaveEdgesAsync(graph.Edges, cancellationToken);

        // Create and start the incremental update service
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var fileWatcher = new DebouncedFileChangeWatcher();
        var fileAnalyzer = new IncrementalFileAnalyzer();

        var updateService = new IncrementalGraphUpdateService(
            workspace,
            _repository,
            graph,
            dependencyTracker,
            fileAnalyzer,
            fileWatcher,
            visitLocals);

        await updateService.StartAsync(cancellationToken);

        return updateService;
    }

    private static string GetTopLevelNamespace(string fullyQualifiedName)
    {
        var dotIndex = fullyQualifiedName.IndexOf('.');
        return dotIndex > 0 ? fullyQualifiedName[..dotIndex] : fullyQualifiedName;
    }

    private static void EnsureMSBuildRegistered()
    {
        lock (RegistrationLock)
        {
            if (_msBuildRegistered) return;
            MSBuildLocator.RegisterDefaults();
            _msBuildRegistered = true;
        }
    }
}