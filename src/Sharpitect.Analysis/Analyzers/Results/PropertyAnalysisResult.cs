namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Contains the analysis results for a single property.
/// </summary>
public class PropertyAnalysisResult
{
    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the property.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}