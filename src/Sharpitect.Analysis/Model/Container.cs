namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a C4 container - an executable or deployable unit.
/// A container contains one or more components.
/// </summary>
public class Container : IElement
{
    private readonly List<Component> _components = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Container"/> class.
    /// </summary>
    /// <param name="name">The name of the container.</param>
    /// <param name="description">An optional description of the container.</param>
    /// <param name="technology">The technology used by this container (e.g., ASP.NET Core).</param>
    public Container(string name, string? description = null, string? technology = null)
    {
        Name = name;
        Description = description;
        Technology = technology;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <summary>
    /// Gets the technology used by this container.
    /// </summary>
    public string? Technology { get; }

    /// <summary>
    /// Gets the components within this container.
    /// </summary>
    public IReadOnlyList<Component> Components => _components;

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => _components;

    /// <summary>
    /// Adds a component to this container.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void AddComponent(Component component)
    {
        _components.Add(component);
    }
}
