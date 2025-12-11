namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a C4 software system - the highest level of abstraction.
/// A software system contains one or more containers.
/// </summary>
public class SoftwareSystem : IElement
{
    private readonly List<Container> _containers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftwareSystem"/> class.
    /// </summary>
    /// <param name="name">The name of the software system.</param>
    /// <param name="description">An optional description of the software system.</param>
    public SoftwareSystem(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <summary>
    /// Gets the containers within this software system.
    /// </summary>
    public IReadOnlyList<Container> Containers => _containers;

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => _containers;

    /// <summary>
    /// Adds a container to this software system.
    /// </summary>
    /// <param name="container">The container to add.</param>
    public void AddContainer(Container container)
    {
        _containers.Add(container);
    }
}
