using Microsoft.CodeAnalysis;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

// TODO: Either move this back to GraphSolutionAnalyzer or move all DeclarationNode factory methods here.
/// <summary>
/// Creates DeclarationNodes
/// </summary>
public static class DeclarationNodeFactory
{
    public static DeclarationNode CreateSolutionNode(string solutionPath)
    {
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        return new DeclarationNode
        {
            Id = solutionName,
            Name = solutionName,
            Kind = DeclarationKind.Solution,
            FilePath = solutionPath,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1,
            C4Level = C4Level.System
        };
    }

    public static DeclarationNode CreateProjectNode(Project project)
    {
        var projectPath = project.FilePath ?? project.Name;

        return new DeclarationNode
        {
            Id = project.Name,
            Name = project.Name,
            Kind = DeclarationKind.Project,
            FilePath = projectPath,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1,
            C4Level = C4Level.Container
        };
    }
}