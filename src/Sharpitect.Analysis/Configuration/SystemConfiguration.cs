using Sharpitect.Analysis.Configuration.Definitions;

namespace Sharpitect.Analysis.Configuration;

/// <summary>
/// DTO for deserializing .sln.yml YAML files.
/// </summary>
public class SystemConfiguration
{
    /// <summary>
    /// Gets or sets the system definition.
    /// </summary>
    public SystemDefinition? System { get; set; }

    /// <summary>
    /// Gets or sets the people who interact with the system.
    /// </summary>
    public List<PersonDefinition>? People { get; set; }

    /// <summary>
    /// Gets or sets the external systems that this system interacts with.
    /// </summary>
    public List<ExternalSystemDefinition>? ExternalSystems { get; set; }

    /// <summary>
    /// Gets or sets the external containers (databases, caches, etc.).
    /// </summary>
    public List<ExternalContainerDefinition>? ExternalContainers { get; set; }

    /// <summary>
    /// Gets or sets the relationships between elements.
    /// </summary>
    public List<RelationshipDefinition>? Relationships { get; set; }
}