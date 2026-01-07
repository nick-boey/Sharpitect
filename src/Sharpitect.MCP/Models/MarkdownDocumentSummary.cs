namespace Sharpitect.MCP.Models;

/// <summary>
/// Summary of a markdown document in the index.
/// </summary>
public sealed record MarkdownDocumentSummary(
    string Id,
    string Name,
    string? Title,
    int HeadingCount,
    int SectionCount);

/// <summary>
/// Results from listing markdown documents.
/// </summary>
public sealed record MarkdownDocumentList(
    IReadOnlyList<MarkdownDocumentSummary> Documents,
    int TotalCount,
    bool Truncated);
