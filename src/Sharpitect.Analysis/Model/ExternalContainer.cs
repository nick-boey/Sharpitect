namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents an external container (database, cache, etc.) that the system uses.
/// </summary>
public class ExternalContainer : IElement
{
    private static readonly IReadOnlyList<IElement> EmptyChildren = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalContainer"/> class.
    /// </summary>
    /// <param name="name">The name of the external container.</param>
    /// <param name="technology">The technology used (e.g., PostgreSQL, Redis).</param>
    /// <param name="description">An optional description of the external container.</param>
    public ExternalContainer(string name, string? technology = null, string? description = null)
    {
        Name = name;
        Technology = technology;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the technology used by this external container.
    /// </summary>
    public string? Technology { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => EmptyChildren;
}
