namespace Sharpitect.Analysis.Configuration.Definitions;

/// <summary>
/// Defines a person/actor in the .sln.yml file.
/// </summary>
public class PersonDefinition
{
    /// <summary>
    /// Gets or sets the name of the person.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the person.
    /// </summary>
    public string? Description { get; set; }
}