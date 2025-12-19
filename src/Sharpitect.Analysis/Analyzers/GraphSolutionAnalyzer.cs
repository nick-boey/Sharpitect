using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Sharpitect.Analysis.Graph;
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The declaration graph.</returns>
    public async Task<DeclarationGraph> AnalyzeAsync(string solutionPath, CancellationToken cancellationToken = default)
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

        // TODO: Start watching for changes in the MSBuild workspace here and update the graph as required

        return graph;
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