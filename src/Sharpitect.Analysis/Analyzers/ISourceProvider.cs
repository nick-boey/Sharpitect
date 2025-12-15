namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Abstracts source input for testability.
/// Production implementations read from the file system.
/// Test implementations provide in-memory strings.
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    /// Gets the C# source code for a compilation unit.
    /// </summary>
    /// <param name="path">The path to the source file.</param>
    /// <returns>The source code content.</returns>
    string GetSourceCode(string path);

    /// <summary>
    /// Gets the YAML content for a configuration file.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    /// <returns>The YAML content, or null if the file doesn't exist.</returns>
    string? GetYamlConfiguration(string path);

    /// <summary>
    /// Lists all source files in a project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>An enumerable of source file paths.</returns>
    IEnumerable<string> GetSourceFiles(string projectPath);

    /// <summary>
    /// Lists all projects in a solution.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>An enumerable of project file paths.</returns>
    IEnumerable<string> GetProjects(string solutionPath);

    /// <summary>
    /// Determines if a project is an executable (OutputType=Exe).
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>True if the project has OutputType=Exe, false otherwise.</returns>
    bool IsExecutableProject(string projectPath);
}
