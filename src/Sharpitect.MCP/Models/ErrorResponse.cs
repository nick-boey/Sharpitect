namespace Sharpitect.MCP.Models;

/// <summary>
/// Standard error response for all tools.
/// </summary>
public sealed record ErrorResponse(
    bool Error,
    string ErrorCode,
    string Message)
{
    public static ErrorResponse NotFound(string message) =>
        new(true, "NOT_FOUND", message);

    public static ErrorResponse InvalidParameter(string message) =>
        new(true, "INVALID_PARAMETER", message);

    public static ErrorResponse NotAnalyzed(string message) =>
        new(true, "NOT_ANALYZED", message);

    public static ErrorResponse AnalysisError(string message) =>
        new(true, "ANALYSIS_ERROR", message);

    public static ErrorResponse GraphBuilding(string? status = null) =>
        new(true, "GRAPH_BUILDING", status ?? "Graph is being built. Please wait and try again.");

    public static ErrorResponse BuildFailed(string message) =>
        new(true, "BUILD_FAILED", message);

    public static ErrorResponse SemanticSearchUnavailable() =>
        new(true, "SEMANTIC_SEARCH_UNAVAILABLE",
            "Semantic search is not available. The embedding service is not configured or embeddings have not been generated.");
}
