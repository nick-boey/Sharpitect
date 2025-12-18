namespace Sharpitect.MCP.Models;

/// <summary>
/// A usage entry.
/// </summary>
public sealed record UsageEntry(
    string LocationId,
    string LocationName,
    string LocationKind,
    string UsageKind,
    string? FilePath,
    int? LineNumber);

/// <summary>
/// Result of a get_usages operation.
/// </summary>
public sealed record UsagesResult(
    string TargetId,
    IReadOnlyList<UsageEntry> Usages,
    int TotalCount,
    bool Truncated);
