using Microsoft.CodeAnalysis;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Analyzes a single file to produce nodes and edges.
/// </summary>
public interface IIncrementalFileAnalyzer
{
    /// <summary>
    /// Analyzes a single document within a compilation context.
    /// </summary>
    /// <param name="document">The Roslyn document to analyze.</param>
    /// <param name="compilation">The compilation containing the document.</param>
    /// <param name="existingSymbolMappings">Existing symbol display string to node ID mappings.</param>
    /// <param name="existingNodeIds">Set of existing node IDs in the graph.</param>
    /// <param name="visitLocals">Whether to include local variables and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis result containing nodes, edges, and new symbol mappings.</returns>
    Task<FileAnalysisResult> AnalyzeFileAsync(
        Document document,
        Compilation compilation,
        IReadOnlyDictionary<string, string> existingSymbolMappings,
        HashSet<string> existingNodeIds,
        bool visitLocals = false,
        CancellationToken cancellationToken = default);
}
