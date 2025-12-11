namespace Sharpitect.Analysis.Model;

/// <summary>
/// Base interface for all C4 model elements.
/// Provides a common contract for navigating the element tree.
/// </summary>
public interface IElement
{
    /// <summary>
    /// Gets the name of the element.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the element.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the child elements contained within this element.
    /// </summary>
    IReadOnlyList<IElement> Children { get; }
}
