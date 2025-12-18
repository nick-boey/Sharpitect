namespace Sharpitect.Analysis.Graph;

/// <summary>
/// Enumeration of all tracked relationship kinds between declaration nodes.
/// </summary>
public enum RelationshipKind
{
    /// <summary>
    /// Structural containment (namespace→type, type→member, method→local).
    /// </summary>
    Contains,

    /// <summary>
    /// Class extends another class.
    /// </summary>
    Inherits,

    /// <summary>
    /// Type implements an interface.
    /// </summary>
    Implements,

    /// <summary>
    /// Method invokes another method.
    /// </summary>
    Calls,

    /// <summary>
    /// Method uses 'new' to construct a type.
    /// </summary>
    Constructs,

    /// <summary>
    /// Type reference (field type, return type, parameter type).
    /// </summary>
    References,

    /// <summary>
    /// Member access (field/property read or write).
    /// </summary>
    Uses,

    /// <summary>
    /// Method overrides a base class method.
    /// </summary>
    Overrides,

    /// <summary>
    /// Project references another project.
    /// </summary>
    DependsOn,

    /// <summary>
    /// Using directive imports a namespace.
    /// </summary>
    Imports
}
