namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// In-memory implementation of <see cref="ISourceProvider"/> for unit testing.
/// Allows tests to provide source code as string literals.
/// </summary>
public class InMemorySourceProvider : ISourceProvider
{
    private readonly Dictionary<string, string> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _yamlConfigs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _projectFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _solutionProjects = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a source file with the specified content.
    /// </summary>
    /// <param name="path">The path to the source file.</param>
    /// <param name="content">The source code content.</param>
    public void AddSource(string path, string content)
    {
        _sources[path] = content;
    }

    /// <summary>
    /// Adds a YAML configuration file with the specified content.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    /// <param name="content">The YAML content.</param>
    public void AddYaml(string path, string content)
    {
        _yamlConfigs[path] = content;
    }

    /// <summary>
    /// Registers a project as part of a solution.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="projectPath">The path to the project file.</param>
    public void AddProject(string solutionPath, string projectPath)
    {
        if (!_solutionProjects.TryGetValue(solutionPath, out var projects))
        {
            projects = new List<string>();
            _solutionProjects[solutionPath] = projects;
        }

        projects.Add(projectPath);
    }

    /// <summary>
    /// Registers a source file as part of a project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="sourceFile">The path to the source file.</param>
    public void AddSourceFile(string projectPath, string sourceFile)
    {
        if (!_projectFiles.TryGetValue(projectPath, out var files))
        {
            files = new List<string>();
            _projectFiles[projectPath] = files;
        }

        files.Add(sourceFile);
    }

    /// <inheritdoc />
    public string GetSourceCode(string path)
    {
        return _sources.TryGetValue(path, out var source) ? source : string.Empty;
    }

    /// <inheritdoc />
    public string? GetYamlConfiguration(string path)
    {
        return _yamlConfigs.TryGetValue(path, out var yaml) ? yaml : null;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSourceFiles(string projectPath)
    {
        return _projectFiles.TryGetValue(projectPath, out var files) ? files : Enumerable.Empty<string>();
    }

    /// <inheritdoc />
    public IEnumerable<string> GetProjects(string solutionPath)
    {
        return _solutionProjects.TryGetValue(solutionPath, out var projects) ? projects : Enumerable.Empty<string>();
    }
}
