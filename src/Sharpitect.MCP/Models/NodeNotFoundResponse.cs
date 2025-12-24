namespace Sharpitect.MCP.Models;

/// <summary>
/// Error response when a node is not found, with suggestions for similar nodes.
/// </summary>
public sealed record NodeNotFoundResponse(
    string Message,
    IReadOnlyList<NodeSummary> SimilarNodes)
{
    /// <summary>
    /// Indicates this is an error response.
    /// </summary>
    public bool Error => true;

    /// <summary>
    /// The error code for not found responses.
    /// </summary>
    public string ErrorCode => "NOT_FOUND";
}
