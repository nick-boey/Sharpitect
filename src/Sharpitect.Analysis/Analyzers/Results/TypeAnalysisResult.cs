namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Contains the analysis results for a single type (class or interface).
/// </summary>
public class TypeAnalysisResult
{
    /// <summary>
    /// Gets or sets the name of the type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace containing the type.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets whether this type is an interface.
    /// </summary>
    public bool IsInterface { get; set; }

    /// <summary>
    /// Gets or sets whether this type is a class.
    /// </summary>
    public bool IsClass { get; set; }

    /// <summary>
    /// Gets or sets the component name from the [Component] attribute, if present.
    /// </summary>
    public string? ComponentName { get; set; }

    /// <summary>
    /// Gets or sets the component description from the [Component] attribute, if present.
    /// </summary>
    public string? ComponentDescription { get; set; }

    /// <summary>
    /// Gets or sets the base types (interfaces and base classes) that this type inherits from.
    /// </summary>
    public List<string> BaseTypes { get; set; } = [];

    /// <summary>
    /// Gets the methods declared in this type.
    /// </summary>
    public List<MethodAnalysisResult> Methods { get; } = [];

    /// <summary>
    /// Gets the properties declared in this type.
    /// </summary>
    public List<PropertyAnalysisResult> Properties { get; } = [];
}