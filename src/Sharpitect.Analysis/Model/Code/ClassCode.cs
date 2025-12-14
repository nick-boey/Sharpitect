namespace Sharpitect.Analysis.Model.Code;

/// <summary>
/// Represents a class in the code-level diagram.
/// A class can contain methods and properties.
/// </summary>
public class ClassCode : ICode
{
    private readonly List<ICode> _members = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassCode"/> class.
    /// </summary>
    /// <param name="name">The name of the class.</param>
    /// <param name="namespace">The namespace containing the class.</param>
    /// <param name="description">An optional description of the class.</param>
    public ClassCode(string name, string? @namespace = null, string? description = null)
    {
        Name = name;
        Namespace = @namespace;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the namespace containing this class.
    /// </summary>
    public string? Namespace { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <summary>
    /// Gets the members (methods and properties) within this class.
    /// </summary>
    public IReadOnlyList<ICode> Members => _members;

    /// <inheritdoc />
    public IReadOnlyList<IElement> Children => _members;

    /// <summary>
    /// Adds a method to this class.
    /// </summary>
    /// <param name="method">The method to add.</param>
    public void AddMethod(MethodCode method)
    {
        _members.Add(method);
    }

    /// <summary>
    /// Adds a property to this class.
    /// </summary>
    /// <param name="property">The property to add.</param>
    public void AddProperty(PropertyCode property)
    {
        _members.Add(property);
    }
}