namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a method in the code-level diagram.
/// Methods are leaf nodes and do not contain children.
/// </summary>
public class MethodCode : ICode
{
    private static readonly IReadOnlyList<IElement> EmptyChildren = Array.Empty<IElement>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodCode"/> class.
    /// </summary>
    /// <param name="name">The name of the method.</param>
    /// <param name="description">An optional description of the method.</param>
    public MethodCode(string name, string? description = null)
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
