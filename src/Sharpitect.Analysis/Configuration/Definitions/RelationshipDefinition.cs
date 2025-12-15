namespace Sharpitect.Analysis.Configuration.Definitions;

/// <summary>
/// Defines a relationship between two elements in the .sln.c4 file.
/// </summary>
public class RelationshipDefinition
{
    /// <summary>
    /// Gets or sets the name of the source element.
    /// </summary>
    public string Start { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action/description of the relationship.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the destination element.
    /// </summary>
    public string End { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional technology used for the relationship.
    /// </summary>
    public string? Technology { get; set; }
}