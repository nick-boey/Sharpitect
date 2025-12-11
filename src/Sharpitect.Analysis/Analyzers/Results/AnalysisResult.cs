namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Contains the results of analyzing source code.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Gets the types discovered during analysis.
    /// </summary>
    public List<TypeAnalysisResult> Types { get; } = [];
}