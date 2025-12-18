namespace Sharpitect.MCP.Models;

/// <summary>
/// Result of a get_relationships operation.
/// </summary>
public sealed record RelationshipsResult(
    string NodeId,
    IReadOnlyList<RelationshipInfo> Outgoing,
    IReadOnlyList<IncomingRelationshipInfo> Incoming);
