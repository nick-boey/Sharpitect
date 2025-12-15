using System.Text.RegularExpressions;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// File system implementation of <see cref="ISourceProvider"/>.
/// Reads source code, YAML configurations, and project/solution files from disk.
/// </summary>
public partial class FileSystemSourceProvider : ISourceProvider
{
    /// <inheritdoc />
    public string GetSourceCode(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <inheritdoc />
    public string? GetYamlConfiguration(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSourceFiles(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
        {
            return [];
        }

        return Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
    }

    /// <inheritdoc />
    public IEnumerable<string> GetProjects(string solutionPath)
    {
        if (!File.Exists(solutionPath))
        {
            return [];
        }

        var solutionDir = Path.GetDirectoryName(solutionPath) ?? string.Empty;
        var content = File.ReadAllText(solutionPath);

        // Match Project lines in .sln file: Project("{...}") = "Name", "Path\To\Project.csproj", "{...}"
        var matches = ProjectLineRegex().Matches(content);

        return matches
            .Select(m => m.Groups[1].Value)
            .Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetFullPath(Path.Combine(solutionDir, p.Replace('\\', Path.DirectorySeparatorChar))));
    }

    [GeneratedRegex(@"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+)""")]
    private static partial Regex ProjectLineRegex();
}
