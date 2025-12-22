namespace Sharpitect.Analysis;

/// <summary>
/// Provides utilities for converting file paths to be relative to the solution root.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Converts an absolute path to a path relative to the solution root directory.
    /// </summary>
    /// <param name="absolutePath">The absolute path to convert.</param>
    /// <param name="solutionRootDirectory">The solution root directory (parent of .sln file).</param>
    /// <returns>The relative path, or the original path if it cannot be made relative.</returns>
    public static string ToRelativePath(string absolutePath, string solutionRootDirectory)
    {
        if (string.IsNullOrEmpty(absolutePath) || string.IsNullOrEmpty(solutionRootDirectory))
        {
            return absolutePath;
        }

        // Normalize paths
        var normalizedAbsolute = Path.GetFullPath(absolutePath);
        var normalizedRoot = Path.GetFullPath(solutionRootDirectory);

        // Ensure root ends with directory separator for proper prefix matching
        if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            normalizedRoot += Path.DirectorySeparatorChar;
        }

        // Check if the path is under the solution root
        if (normalizedAbsolute.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedAbsolute[normalizedRoot.Length..];
        }

        // Path is outside solution root - use Path.GetRelativePath for proper relative path
        return Path.GetRelativePath(solutionRootDirectory, absolutePath);
    }

    /// <summary>
    /// Gets the solution root directory from a solution file path.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <returns>The directory containing the solution file.</returns>
    public static string GetSolutionRootDirectory(string solutionPath)
    {
        return Path.GetDirectoryName(Path.GetFullPath(solutionPath))
               ?? throw new ArgumentException("Invalid solution path", nameof(solutionPath));
    }
}
