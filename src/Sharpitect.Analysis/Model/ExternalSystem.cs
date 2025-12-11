namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents an external software system that interacts with the analyzed system.
/// </summary>
public class ExternalSystem : IElement
{
    private static readonly IReadOnlyList<IElement> EmptyChildren = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalSystem"/> class.
    /// </summary>
    /// <param name="name">The name of the external system.</param>
    /// <param name="description">An optional description of the external system.</param>
    public ExternalSystem(string name, string? description = null)
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
