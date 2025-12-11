namespace Sharpitect.Analysis.Model;

/// <summary>
/// The complete architecture model extracted from analysis.
/// Contains all elements and relationships discovered.
/// </summary>
public class ArchitectureModel
{
    private readonly List<SoftwareSystem> _systems = new();
    private readonly List<Person> _people = new();
    private readonly List<ExternalSystem> _externalSystems = new();
    private readonly List<ExternalContainer> _externalContainers = new();
    private readonly List<Relationship> _relationships = new();

    /// <summary>
    /// Gets the software systems in this architecture model.
    /// </summary>
    public IReadOnlyList<SoftwareSystem> Systems => _systems;

    /// <summary>
    /// Gets the people/actors in this architecture model.
    /// </summary>
    public IReadOnlyList<Person> People => _people;

    /// <summary>
    /// Gets the external systems in this architecture model.
    /// </summary>
    public IReadOnlyList<ExternalSystem> ExternalSystems => _externalSystems;

    /// <summary>
    /// Gets the external containers in this architecture model.
    /// </summary>
    public IReadOnlyList<ExternalContainer> ExternalContainers => _externalContainers;

    /// <summary>
    /// Gets the relationships between elements in this architecture model.
    /// </summary>
    public IReadOnlyList<Relationship> Relationships => _relationships;

    /// <summary>
    /// Adds a software system to this architecture model.
    /// </summary>
    /// <param name="system">The software system to add.</param>
    public void AddSystem(SoftwareSystem system) => _systems.Add(system);

    /// <summary>
    /// Adds a person to this architecture model.
    /// </summary>
    /// <param name="person">The person to add.</param>
    public void AddPerson(Person person) => _people.Add(person);

    /// <summary>
    /// Adds an external system to this architecture model.
    /// </summary>
    /// <param name="externalSystem">The external system to add.</param>
    public void AddExternalSystem(ExternalSystem externalSystem) => _externalSystems.Add(externalSystem);

    /// <summary>
    /// Adds an external container to this architecture model.
    /// </summary>
    /// <param name="externalContainer">The external container to add.</param>
    public void AddExternalContainer(ExternalContainer externalContainer) => _externalContainers.Add(externalContainer);

    /// <summary>
    /// Adds a relationship to this architecture model.
    /// </summary>
    /// <param name="relationship">The relationship to add.</param>
    public void AddRelationship(Relationship relationship) => _relationships.Add(relationship);
}
