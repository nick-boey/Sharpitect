using Microsoft.CodeAnalysis;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Results from semantic analysis of a project.
/// </summary>
public sealed class ProjectAnalysisResult
{
    /// <summary>
    /// Gets the declaration nodes discovered in the project.
    /// </summary>
    public required IReadOnlyList<DeclarationNode> Nodes { get; init; }

    /// <summary>
    /// Gets the relationship edges discovered in the project.
    /// </summary>
    public required IReadOnlyList<RelationshipEdge> Edges { get; init; }

    /// <summary>
    /// Gets the symbol-to-node-ID mapping for cross-project reference resolution.
    /// </summary>
    public required IReadOnlyDictionary<ISymbol, string> SymbolToNodeId { get; init; }
}

/// <summary>
/// Analyzes a single project using Roslyn semantic analysis.
/// </summary>
public sealed class SemanticProjectAnalyzer
{
    /// <summary>
    /// Analyzes a project and extracts declarations and relationships.
    /// </summary>
    /// <param name="project">The Roslyn project to analyze.</param>
    /// <param name="existingSymbolMap">Existing symbol-to-node-ID mapping from previous projects.</param>
    /// <param name="existingNodeIds">Set of existing node IDs for filtering solution-internal references.</param>
    /// <param name="solutionRootDirectory">The solution root directory for creating relative paths.</param>
    /// <param name="visitLocals">True to include locals variables and parameteres in the tree.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results containing nodes and edges.</returns>
    public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
        Project project,
        Dictionary<ISymbol, string> existingSymbolMap,
        HashSet<string> existingNodeIds,
        string solutionRootDirectory,
        bool visitLocals = false,
        CancellationToken cancellationToken = default)
    {
        var allNodes = new List<DeclarationNode>();
        var allEdges = new List<RelationshipEdge>();
        var symbolToNodeId = new Dictionary<ISymbol, string>(existingSymbolMap, SymbolEqualityComparer.Default);

        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            return new ProjectAnalysisResult
            {
                Nodes = allNodes,
                Edges = allEdges,
                SymbolToNodeId = symbolToNodeId
            };
        }

        // First pass: collect all declarations
        foreach (var document in project.Documents)
        {
            if (document.FilePath == null) continue;

            var relativePath = PathHelper.ToRelativePath(document.FilePath, solutionRootDirectory);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntaxTree!);

            var declarationVisitor = new DeclarationVisitor(semanticModel, relativePath, visitLocals);
            declarationVisitor.Visit(await syntaxTree!.GetRootAsync(cancellationToken));

            allNodes.AddRange(declarationVisitor.Nodes);
            allEdges.AddRange(declarationVisitor.ContainmentEdges);

            // Merge symbol mappings
            foreach (var kvp in declarationVisitor.SymbolToNodeId)
            {
                symbolToNodeId[kvp.Key] = kvp.Value;
            }
        }

        // Update the set of all node IDs
        var allNodeIds = new HashSet<string>(existingNodeIds);
        foreach (var node in allNodes)
        {
            allNodeIds.Add(node.Id);
        }

        // Second pass: collect references and relationships
        foreach (var document in project.Documents)
        {
            if (document.FilePath == null) continue;

            var relativePath = PathHelper.ToRelativePath(document.FilePath, solutionRootDirectory);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntaxTree!);

            var referenceVisitor = new ReferenceVisitor(
                semanticModel,
                relativePath,
                symbolToNodeId,
                allNodeIds);

            referenceVisitor.Visit(await syntaxTree!.GetRootAsync(cancellationToken));
            allEdges.AddRange(referenceVisitor.Edges);
        }

        // Third pass: extract TODO/FIXME/HACK/XXX comments
        foreach (var document in project.Documents)
        {
            if (document.FilePath == null) continue;

            var relativePath = PathHelper.ToRelativePath(document.FilePath, solutionRootDirectory);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntaxTree!);

            var todoVisitor = new TodoCommentVisitor(semanticModel, relativePath, symbolToNodeId);
            todoVisitor.Visit(await syntaxTree!.GetRootAsync(cancellationToken));

            allNodes.AddRange(todoVisitor.TodoNodes);
            allEdges.AddRange(todoVisitor.ContainmentEdges);
        }

        return new ProjectAnalysisResult
        {
            Nodes = allNodes,
            Edges = allEdges,
            SymbolToNodeId = symbolToNodeId
        };
    }
}