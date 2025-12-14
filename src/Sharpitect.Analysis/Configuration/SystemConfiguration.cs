namespace Sharpitect.Analysis.Configuration;

/// <summary>
/// DTO for deserializing .sln.c4 YAML files.
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

/// <summary>
/// Defines a relationship between two elements in the .sln.c4 file.
/// </summary>
public class RelationshipDefinition
{
    /// <summary>
    /// Gets or sets the name of the source element.
    /// </summary>
    public string Start { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action/description of the relationship.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the destination element.
    /// </summary>
    public string End { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional technology used for the relationship.
    /// </summary>
    public string? Technology { get; set; }
}

/// <summary>
/// Defines a software system in the .sln.c4 file.
/// </summary>
public class SystemDefinition
{
    /// <summary>
    /// Gets or sets the name of the system.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the system.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Defines a person/actor in the .sln.c4 file.
/// </summary>
public class PersonDefinition
{
    /// <summary>
    /// Gets or sets the name of the person.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the person.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Defines an external system in the .sln.c4 file.
/// </summary>
public class ExternalSystemDefinition
{
    /// <summary>
    /// Gets or sets the name of the external system.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the external system.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Defines an external container in the .sln.c4 file.
/// </summary>
public class ExternalContainerDefinition
{
    /// <summary>
    /// Gets or sets the name of the external container.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the technology used by the external container.
    /// </summary>
    public string? Technology { get; set; }

    /// <summary>
    /// Gets or sets the description of the external container.
    /// </summary>
    public string? Description { get; set; }
}
