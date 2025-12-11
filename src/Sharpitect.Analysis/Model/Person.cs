namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a person/actor who interacts with the software system.
/// </summary>
public class Person : IElement
{
    private static readonly IReadOnlyList<IElement> EmptyChildren = Array.Empty<IElement>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Person"/> class.
    /// </summary>
    /// <param name="name">The name of the person.</param>
    /// <param name="description">An optional description of the person.</param>
    public Person(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => EmptyChildren;
}
