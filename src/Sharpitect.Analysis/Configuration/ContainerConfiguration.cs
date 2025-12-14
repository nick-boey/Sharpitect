namespace Sharpitect.Analysis.Configuration;

/// <summary>
/// DTO for deserializing .csproj.c4 YAML files.
/// </summary>
public class ContainerConfiguration
{
    /// <summary>
    /// Gets or sets the container definition.
    /// </summary>
    public ContainerDefinition? Container { get; init; }

    /// <summary>
    /// Gets or sets the namespace-based component mappings.
    /// </summary>
    public List<ComponentDefinition>? Components { get; set; }
}