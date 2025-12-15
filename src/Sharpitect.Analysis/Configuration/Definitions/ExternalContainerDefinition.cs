namespace Sharpitect.Analysis.Configuration;

/// <summary>
/// Defines an external container in the .sln.c4 file.
/// </summary>
public class ExternalContainerDefinition
{
    /// <summary>
    /// Gets or sets the name of the external container.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the technology used by the external container.
    /// </summary>
    public string? Technology { get; set; }

    /// <summary>
    /// Gets or sets the description of the external container.
    /// </summary>
    public string? Description { get; set; }
}