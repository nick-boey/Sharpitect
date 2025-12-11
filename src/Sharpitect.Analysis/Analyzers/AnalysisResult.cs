namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Contains the results of analyzing source code.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Gets the types discovered during analysis.
    /// </summary>
    public List<TypeAnalysisResult> Types { get; } = new();
}

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
    public List<string> BaseTypes { get; set; } = new();

    /// <summary>
    /// Gets the methods declared in this type.
    /// </summary>
    public List<MethodAnalysisResult> Methods { get; } = new();

    /// <summary>
    /// Gets the properties declared in this type.
    /// </summary>
    public List<PropertyAnalysisResult> Properties { get; } = new();
}

/// <summary>
/// Contains the analysis results for a single method.
/// </summary>
public class MethodAnalysisResult
{
    /// <summary>
    /// Gets or sets the name of the method.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the method.
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action name from the [Action] attribute, if present.
    /// </summary>
    public string? ActionName { get; set; }

    /// <summary>
    /// Gets or sets the person name from the [UserAction] attribute, if present.
    /// </summary>
    public string? UserActionPerson { get; set; }

    /// <summary>
    /// Gets or sets the action description from the [UserAction] attribute, if present.
    /// </summary>
    public string? UserActionDescription { get; set; }
}

/// <summary>
/// Contains the analysis results for a single property.
/// </summary>
public class PropertyAnalysisResult
{
    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the property.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
