using Sharpitect.Analysis.Graph;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Services;

/// <summary>
/// Result of attempting to resolve a node by identifier.
/// </summary>
public abstract record NodeResolutionResult
{
    /// <summary>
    /// Indicates whether the resolution was successful.
    /// </summary>
    public bool IsResolved => this is Resolved;

    /// <summary>
    /// The node was found with an exact match.
    /// </summary>
    public sealed record Resolved(DeclarationNode Node) : NodeResolutionResult;

    /// <summary>
    /// The node was not found. Contains similar nodes if any were found.
    /// </summary>
    public sealed record NotResolved(IReadOnlyList<NodeSummary> SimilarNodes) : NodeResolutionResult;
}
