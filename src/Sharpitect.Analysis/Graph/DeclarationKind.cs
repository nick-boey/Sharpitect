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
    TodoComment
}