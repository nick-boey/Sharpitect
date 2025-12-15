namespace Sharpitect.Analysis.Configuration.Definitions;

/// <summary>
/// Defines a container in the .csproj.c4 file.
/// </summary>
public class ContainerDefinition
{
    /// <summary>
    /// Gets or sets the name of the container.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the container.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the technology used by the container.
    /// </summary>
    public string? Technology { get; init; }
}