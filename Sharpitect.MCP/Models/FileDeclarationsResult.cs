namespace Sharpitect.MCP.Models;

/// <summary>
/// A declaration with optional children.
/// </summary>
public sealed record DeclarationWithChildren(
    string Id,
    string Name,
    string Kind,
    int? LineNumber,
    IReadOnlyList<DeclarationWithChildren> Children);

/// <summary>
/// Result of a get_file_declarations operation.
/// </summary>
public sealed record FileDeclarationsResult(
    string FilePath,
    IReadOnlyList<DeclarationWithChildren> Declarations);
