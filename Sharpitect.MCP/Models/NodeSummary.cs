using Sharpitect.Analysis.Graph;

namespace Sharpitect.MCP.Models;

/// <summary>
/// Lightweight summary of a declaration node for list results.
/// </summary>
public sealed record NodeSummary(
    string Id,
    string Name,
    string Kind,
    string? C4Level,
    string? FilePath,
    int? LineNumber)
{
    public static NodeSummary FromDeclarationNode(DeclarationNode node) =>
        new(
            node.Id,
            node.Name,
            node.Kind.ToString(),
            node.C4Level == Analysis.Graph.C4Level.None ? null : node.C4Level.ToString(),
            node.FilePath,
            node.StartLine > 0 ? node.StartLine : null);
}
