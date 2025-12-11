namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a relationship between two C4 model elements.
/// </summary>
public class Relationship
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Relationship"/> class.
    /// </summary>
    /// <param name="source">The source element of the relationship.</param>
    /// <param name="destination">The destination element of the relationship.</param>
    /// <param name="description">The description of the relationship.</param>
    /// <param name="technology">The optional technology used for this relationship.</param>
    public Relationship(IElement source, IElement destination, string description, string? technology = null)
    {
        Source = source;
        Destination = destination;
        Description = description;
        Technology = technology;
    }

    /// <summary>
    /// Gets the source element of the relationship.
    /// </summary>
    public IElement Source { get; }

    /// <summary>
    /// Gets the destination element of the relationship.
    /// </summary>
    public IElement Destination { get; }

    /// <summary>
    /// Gets the description of the relationship (e.g., "processes payment").
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the technology used for this relationship (optional).
    /// </summary>
    public string? Technology { get; }
}
