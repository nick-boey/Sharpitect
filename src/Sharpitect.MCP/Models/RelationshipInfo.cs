using Sharpitect.Analysis.Graph;

namespace Sharpitect.MCP.Models;

/// <summary>
/// Information about a relationship between nodes.
/// </summary>
public sealed record RelationshipInfo(
    string Kind,
    string TargetId,
    string TargetName,
    string TargetKind)
{
    public static RelationshipInfo FromEdge(RelationshipEdge edge, DeclarationNode targetNode) =>
        new(
            edge.Kind.ToString(),
            targetNode.Id,
            targetNode.Name,
            targetNode.Kind.ToString());
}

/// <summary>
/// Information about an incoming relationship.
/// </summary>
public sealed record IncomingRelationshipInfo(
    string Kind,
    string SourceId,
    string SourceName,
    string SourceKind)
{
    public static IncomingRelationshipInfo FromEdge(RelationshipEdge edge, DeclarationNode sourceNode) =>
        new(
            edge.Kind.ToString(),
            sourceNode.Id,
            sourceNode.Name,
            sourceNode.Kind.ToString());
}
