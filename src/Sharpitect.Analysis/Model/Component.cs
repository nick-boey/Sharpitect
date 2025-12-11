namespace Sharpitect.Analysis.Model;

/// <summary>
/// Represents a C4 component - a logical grouping of related functionality.
/// A component contains code-level elements (classes, methods, properties).
/// </summary>
public class Component : IElement
{
    private readonly List<ICode> _codeElements = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Component"/> class.
    /// </summary>
    /// <param name="name">The name of the component.</param>
    /// <param name="description">An optional description of the component.</param>
    public Component(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <summary>
    /// Gets the code elements within this component.
    /// </summary>
    public IReadOnlyList<ICode> CodeElements => _codeElements;

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => _codeElements;

    /// <summary>
    /// Adds a code element to this component.
    /// </summary>
    /// <param name="codeElement">The code element to add.</param>
    public void AddCodeElement(ICode codeElement)
    {
        _codeElements.Add(codeElement);
    }
}
