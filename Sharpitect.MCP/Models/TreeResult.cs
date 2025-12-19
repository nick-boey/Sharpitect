namespace Sharpitect.MCP.Models;

/// <summary>
/// A node in the containment tree with its children.
/// </summary>
public sealed record TreeNode(
    string Id,
    string Name,
    string Kind,
    IReadOnlyList<TreeNode> Children);

/// <summary>
/// Result of a tree operation showing containment hierarchy.
/// </summary>
public sealed record TreeResult(
    string? RootId,
    IReadOnlyList<TreeNode> Roots,
    int TotalNodes,
    int MaxDepth);