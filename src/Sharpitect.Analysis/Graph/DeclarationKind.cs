namespace Sharpitect.Analysis.Graph;

/// <summary>
/// Enumeration of all tracked declaration kinds in the code graph.
/// </summary>
public enum DeclarationKind
{
    // Structural declarations
    Solution,
    Project,
    Namespace,

    // Type declarations
    Class,
    Interface,
    Struct,
    Record,
    Enum,
    Delegate,

    // Member declarations
    Method,
    Constructor,
    Property,
    Field,
    Event,
    Indexer,

    // Other declarations
    EnumMember,
    Parameter,
    TypeParameter,
    LocalVariable,
    LocalFunction,

    // Comment markers
    TodoComment,

    // Markdown declarations
    /// <summary>
    /// A markdown document (.md file).
    /// </summary>
    MarkdownDocument,

    /// <summary>
    /// A heading in a markdown document (H1-H6).
    /// </summary>
    MarkdownHeading,

    /// <summary>
    /// A content section under a heading (used for chunking/embedding).
    /// </summary>
    MarkdownSection
}