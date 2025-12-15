namespace Sharpitect.Analysis.Configuration.Definitions;

/// <summary>
/// Defines a namespace-based component mapping in the .csproj.yml file.
/// </summary>
public class ComponentDefinition
{
    /// <summary>
    /// Gets or sets the namespace to map to this component.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Gets or sets the name of the component.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the component.
    /// </summary>
    public string? Description { get; init; }
}