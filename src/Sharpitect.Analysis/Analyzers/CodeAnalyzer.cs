using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Analyzes C# source code using Roslyn to extract components, classes, and relationships.
/// </summary>
public class CodeAnalyzer
{
    /// <summary>
    /// Analyzes source code and returns discovered components and code elements.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    /// <param name="fileName">Optional file name for diagnostics.</param>
    /// <returns>The analysis results containing discovered types.</returns>
    public AnalysisResult AnalyzeSource(string sourceCode, string fileName = "source.cs")
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: fileName);
        var root = tree.GetCompilationUnitRoot();

        var result = new AnalysisResult();

        // Find all type declarations (classes, interfaces)
        var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

        foreach (var typeDecl in typeDeclarations)
        {
            var typeInfo = AnalyzeType(typeDecl);
            result.Types.Add(typeInfo);
        }

        return result;
    }

    private static TypeAnalysisResult AnalyzeType(TypeDeclarationSyntax typeDecl)
    {
        var result = new TypeAnalysisResult
        {
            Name = typeDecl.Identifier.Text,
            Namespace = GetNamespace(typeDecl),
            IsInterface = typeDecl is InterfaceDeclarationSyntax,
            IsClass = typeDecl is ClassDeclarationSyntax
        };

        // Check for [Component] attribute
        var componentAttr = FindAttribute(typeDecl, "Component");
        if (componentAttr != null)
        {
            result.ComponentName = GetAttributeArgument(componentAttr, 0);
            result.ComponentDescription = GetNamedAttributeArgument(componentAttr, "Description");
        }

        // Analyze methods
        foreach (var method in typeDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            var methodInfo = AnalyzeMethod(method);
            result.Methods.Add(methodInfo);
        }

        // Analyze properties
        foreach (var property in typeDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            result.Properties.Add(new PropertyAnalysisResult
            {
                Name = property.Identifier.Text,
                Type = property.Type.ToString()
            });
        }

        // Get base types (for implementation tracking)
        if (typeDecl.BaseList != null)
        {
            result.BaseTypes = typeDecl.BaseList.Types.Select(t => t.Type.ToString()).ToList();
        }

        return result;
    }

    private static MethodAnalysisResult AnalyzeMethod(MethodDeclarationSyntax method)
    {
        var result = new MethodAnalysisResult
        {
            Name = method.Identifier.Text,
            ReturnType = method.ReturnType.ToString()
        };

        // Check for [Action] attribute
        var actionAttr = FindAttribute(method, "Action");
        if (actionAttr != null)
        {
            result.ActionName = GetAttributeArgument(actionAttr, 0);
        }

        // Check for [UserAction] attribute
        var userActionAttr = FindAttribute(method, "UserAction");
        if (userActionAttr != null)
        {
            result.UserActionPerson = GetAttributeArgument(userActionAttr, 0);
            result.UserActionDescription = GetAttributeArgument(userActionAttr, 1);
        }

        return result;
    }

    private static string? GetNamespace(TypeDeclarationSyntax typeDecl)
    {
        // Handle file-scoped namespaces
        var fileScopedNs = typeDecl.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScopedNs != null)
        {
            return fileScopedNs.Name.ToString();
        }

        // Handle traditional namespaces
        var ns = typeDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return ns?.Name.ToString();
    }

    private static AttributeSyntax? FindAttribute(MemberDeclarationSyntax member, string attributeName)
    {
        return member.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a =>
                a.Name.ToString() == attributeName ||
                a.Name.ToString() == attributeName + "Attribute");
    }

    private static string? GetAttributeArgument(AttributeSyntax attr, int index)
    {
        var args = attr.ArgumentList?.Arguments;
        if (args == null || args.Value.Count <= index)
        {
            return null;
        }

        var arg = args.Value[index];
        if (arg.Expression is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }

        return arg.Expression.ToString().Trim('"');
    }

    private static string? GetNamedAttributeArgument(AttributeSyntax attr, string name)
    {
        var namedArg = attr.ArgumentList?.Arguments
            .FirstOrDefault(a => a.NameEquals?.Name.ToString() == name);

        if (namedArg?.Expression is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }

        return namedArg?.Expression?.ToString().Trim('"');
    }
}
