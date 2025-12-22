using Microsoft.CodeAnalysis;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

// TODO: Either move this back to GraphSolutionAnalyzer or move all DeclarationNode factory methods here.
/// <summary>
/// Creates DeclarationNodes
/// </summary>
public static class DeclarationNodeFactory
{
    public static DeclarationNode CreateSolutionNode(string solutionPath, string solutionRootDirectory)
    {
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        var relativePath = PathHelper.ToRelativePath(solutionPath, solutionRootDirectory);

        return new DeclarationNode
        {
            Id = solutionName,
            Name = solutionName,
            Kind = DeclarationKind.Solution,
            FilePath = relativePath,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1,
            C4Level = C4Level.System
        };
    }

    public static DeclarationNode CreateProjectNode(Project project, string solutionRootDirectory)
    {
        var projectPath = project.FilePath ?? project.Name;
        var relativePath = PathHelper.ToRelativePath(projectPath, solutionRootDirectory);

        return new DeclarationNode
        {
            Id = project.Name,
            Name = project.Name,
            Kind = DeclarationKind.Project,
            FilePath = relativePath,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1,
            C4Level = C4Level.Container
        };
    }
}