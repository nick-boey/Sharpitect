namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a property in the code-level diagram.
/// Properties are leaf nodes and do not contain children.
/// </summary>
public class PropertyCode : ICode
{
    private static readonly IReadOnlyList<IElement> EmptyChildren = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyCode"/> class.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="type">The type of the property.</param>
    /// <param name="description">An optional description of the property.</param>
    public PropertyCode(string name, string? type = null, string? description = null)
    {
        Name = name;
        Type = type;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the type of this property.
    /// </summary>
    public string? Type { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => EmptyChildren;
}
