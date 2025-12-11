namespace Sharpitect.Analysis.Configuration;

/// <summary>
/// DTO for deserializing .csproj.c4 YAML files.
/// </summary>
public class ContainerConfiguration
{
    /// <summary>
    /// Gets or sets the container definition.
    /// </summary>
    public ContainerDefinition? Container { get; set; }

    /// <summary>
    /// Gets or sets the namespace-based component mappings.
    /// </summary>
    public List<ComponentDefinition>? Components { get; set; }
}

/// <summary>
/// Defines a container in the .csproj.c4 file.
/// </summary>
public class ContainerDefinition
{
    /// <summary>
    /// Gets or sets the name of the container.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the container.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the technology used by the container.
    /// </summary>
    public string? Technology { get; set; }
}

/// <summary>
/// Defines a namespace-based component mapping in the .csproj.c4 file.
/// </summary>
public class ComponentDefinition
{
    /// <summary>
    /// Gets or sets the namespace to map to this component.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the component.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the component.
    /// </summary>
    public string? Description { get; set; }
}
