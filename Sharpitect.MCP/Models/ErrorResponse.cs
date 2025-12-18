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
}
