using Microsoft.CodeAnalysis;
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Analyzes a single file to produce nodes and edges incrementally.
/// </summary>
public sealed class IncrementalFileAnalyzer : IIncrementalFileAnalyzer
{
    /// <inheritdoc />
    public async Task<FileAnalysisResult> AnalyzeFileAsync(
        Document document,
        Compilation compilation,
        IReadOnlyDictionary<string, string> existingSymbolMappings,
        HashSet<string> existingNodeIds,
        string solutionRootDirectory,
        bool visitLocals = false,
        CancellationToken cancellationToken = default)
    {
        if (document.FilePath == null)
        {
            return new FileAnalysisResult
            {
                Nodes = [],
                Edges = [],
                SymbolMappings = new Dictionary<string, string>()
            };
        }

        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
        if (syntaxTree == null)
        {
            return new FileAnalysisResult
            {
                Nodes = [],
                Edges = [],
                SymbolMappings = new Dictionary<string, string>()
            };
        }

        var relativePath = PathHelper.ToRelativePath(document.FilePath, solutionRootDirectory);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync(cancellationToken);

        // First pass: Extract declarations
        var declarationVisitor = new DeclarationVisitor(semanticModel, relativePath, visitLocals);
        declarationVisitor.Visit(root);

        var nodes = declarationVisitor.Nodes;
        var edges = new List<RelationshipEdge>(declarationVisitor.ContainmentEdges);

        // Build symbol mapping from existing + new
        var symbolToNodeId = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        foreach (var kvp in declarationVisitor.SymbolToNodeId)
        {
            symbolToNodeId[kvp.Key] = kvp.Value;
        }

        // Also consider existing symbol mappings by display string
        // We can't directly add ISymbol mappings from the existing ones since they're
        // from a different compilation, but we'll use them for node ID lookup

        // Build combined node ID set for reference resolution
        var allNodeIds = new HashSet<string>(existingNodeIds);
        foreach (var node in nodes)
        {
            allNodeIds.Add(node.Id);
        }

        // Second pass: Extract references
        var referenceVisitor = new ReferenceVisitor(
            semanticModel,
            relativePath,
            symbolToNodeId,
            allNodeIds);
        referenceVisitor.Visit(root);

        edges.AddRange(referenceVisitor.Edges);

        // Convert symbol mappings to display string based mapping
        var symbolMappings = new Dictionary<string, string>();
        foreach (var kvp in declarationVisitor.SymbolToNodeId)
        {
            var displayString = kvp.Key.ToDisplayString();
            symbolMappings[displayString] = kvp.Value;
        }

        return new FileAnalysisResult
        {
            Nodes = nodes,
            Edges = edges,
            SymbolMappings = symbolMappings
        };
    }
}
