namespace Sharpitect.Analysis.Configuration.Definitions;

/// <summary>
/// Defines a software system in the .sln.c4 file.
/// </summary>
public class SystemDefinition
{
    /// <summary>
    /// Gets or sets the name of the system.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the system.
    /// </summary>
    public string? Description { get; set; }
}