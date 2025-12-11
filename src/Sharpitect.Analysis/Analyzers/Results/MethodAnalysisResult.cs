namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Contains the analysis results for a single method.
/// </summary>
public class MethodAnalysisResult
{
    /// <summary>
    /// Gets or sets the name of the method.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the method.
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action name from the [Action] attribute, if present.
    /// </summary>
    public string? ActionName { get; set; }

    /// <summary>
    /// Gets or sets the person name from the [UserAction] attribute, if present.
    /// </summary>
    public string? UserActionPerson { get; set; }

    /// <summary>
    /// Gets or sets the action description from the [UserAction] attribute, if present.
    /// </summary>
    public string? UserActionDescription { get; set; }
}