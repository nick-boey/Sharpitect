namespace Sharpitect.MCP.Models;

/// <summary>
/// Represents a heading in a markdown document.
/// </summary>
public sealed record MarkdownHeadingSummary(
    string Id,
    string Text,
    int Level,
    int LineNumber);

/// <summary>
/// Represents a link from a markdown document.
/// </summary>
public sealed record MarkdownLinkSummary(
    string TargetId,
    string? LinkText,
    bool IsWikilink,
    int SourceLine);

/// <summary>
/// Detailed information about a markdown document.
/// </summary>
public sealed record MarkdownDocumentDetail(
    string Id,
    string Name,
    string? Title,
    string? ContentHash,
    IReadOnlyList<MarkdownHeadingSummary> Headings,
    IReadOnlyList<MarkdownLinkSummary> OutgoingLinks,
    IReadOnlyList<MarkdownLinkSummary> IncomingLinks,
    int SectionCount);
