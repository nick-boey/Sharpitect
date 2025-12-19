using Sharpitect.Analysis.Graph;

namespace Sharpitect.MCP.Models;

/// <summary>
/// Detailed information about a declaration node.
/// </summary>
public sealed record NodeDetail(
    string Id,
    string Name,
    string Kind,
    string? C4Level,
    string? FilePath,
    int? LineNumber,
    int? EndLineNumber,
    string? Metadata)
{
    public static NodeDetail FromDeclarationNode(DeclarationNode node) =>
        new(
            node.Id,
            node.Name,
            node.Kind.ToString(),
            node.C4Level == Analysis.Graph.C4Level.None ? null : node.C4Level.ToString(),
            node.FilePath,
            node.StartLine > 0 ? node.StartLine : null,
            node.EndLine > 0 ? node.EndLine : null,
            node.Metadata);
}