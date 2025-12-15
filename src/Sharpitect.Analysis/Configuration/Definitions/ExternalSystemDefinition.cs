namespace Sharpitect.Analysis.Configuration;

/// <summary>
/// Defines an external system in the .sln.c4 file.
/// </summary>
public class ExternalSystemDefinition
{
    /// <summary>
    /// Gets or sets the name of the external system.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the external system.
    /// </summary>
    public string? Description { get; set; }
}