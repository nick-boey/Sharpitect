using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Calculates code metrics such as cyclomatic complexity, lines of code, and method counts.
/// </summary>
public sealed class CodeMetricsVisitor : CSharpSyntaxWalker
{
    private readonly string _filePath;
    private readonly SyntaxTree _syntaxTree;
    private readonly Dictionary<ISymbol, string> _symbolToNodeId;
    private readonly SemanticModel _semanticModel;

    /// <summary>
    /// Gets all discovered code metrics nodes.
    /// </summary>
    public List<DeclarationNode> MetricsNodes { get; } = [];

    /// <summary>
    /// Gets all containment edges linking metrics to their target declarations.
    /// </summary>
    public List<RelationshipEdge> ContainmentEdges { get; } = [];

    /// <summary>
    /// Creates a new code metrics visitor.
    /// </summary>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="filePath">The file path being analyzed.</param>
    /// <param name="symbolToNodeId">Mapping from symbols to node IDs.</param>
    public CodeMetricsVisitor(
        SemanticModel semanticModel,
        string filePath,
        Dictionary<ISymbol, string> symbolToNodeId)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
        _syntaxTree = semanticModel.SyntaxTree;
        _symbolToNodeId = symbolToNodeId;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        ProcessMemberMetrics(node, node.Body ?? (SyntaxNode?)node.ExpressionBody);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        ProcessMemberMetrics(node, node.Body ?? (SyntaxNode?)node.ExpressionBody);
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        ProcessMemberMetrics(node, node.ExpressionBody);
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ProcessTypeMetrics(node);
        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        ProcessTypeMetrics(node);
        base.VisitStructDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        ProcessTypeMetrics(node);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        ProcessTypeMetrics(node);
        base.VisitInterfaceDeclaration(node);
    }

    private void ProcessMemberMetrics(SyntaxNode declarationNode, SyntaxNode? bodyNode)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(declarationNode);
        if (symbol == null || !_symbolToNodeId.TryGetValue(symbol, out var nodeId))
        {
            return;
        }

        var location = _syntaxTree.GetLineSpan(declarationNode.Span);
        var startLine = location.StartLinePosition.Line + 1;
        var endLine = location.EndLinePosition.Line + 1;

        var linesOfCode = endLine - startLine + 1;
        var cyclomaticComplexity = bodyNode != null ? CalculateCyclomaticComplexity(bodyNode) : 1;

        var metrics = new
        {
            linesOfCode,
            cyclomaticComplexity,
            metricType = "method"
        };

        CreateMetricsNode(nodeId, startLine, metrics);
    }

    private void ProcessTypeMetrics(TypeDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol == null || !_symbolToNodeId.TryGetValue(symbol, out var nodeId))
        {
            return;
        }

        var location = _syntaxTree.GetLineSpan(node.Span);
        var startLine = location.StartLinePosition.Line + 1;
        var endLine = location.EndLinePosition.Line + 1;

        var linesOfCode = endLine - startLine + 1;
        var methodCount = node.Members.Count(m => m is MethodDeclarationSyntax or ConstructorDeclarationSyntax);
        var propertyCount = node.Members.Count(m => m is PropertyDeclarationSyntax);
        var fieldCount = node.Members.Count(m => m is FieldDeclarationSyntax);

        var metrics = new
        {
            linesOfCode,
            methodCount,
            propertyCount,
            fieldCount,
            totalMembers = methodCount + propertyCount + fieldCount,
            metricType = "type"
        };

        CreateMetricsNode(nodeId, startLine, metrics);
    }

    private void CreateMetricsNode(string targetNodeId, int line, object metrics)
    {
        var id = $"{targetNodeId}$METRICS";
        var metadata = JsonSerializer.Serialize(metrics);

        var metricsNode = new DeclarationNode
        {
            Id = id,
            Name = "Code Metrics",
            Kind = DeclarationKind.CodeMetrics,
            FilePath = _filePath,
            StartLine = line,
            StartColumn = 1,
            EndLine = line,
            EndColumn = 1,
            Metadata = metadata
        };

        MetricsNodes.Add(metricsNode);

        ContainmentEdges.Add(new RelationshipEdge
        {
            Id = Guid.NewGuid().ToString(),
            SourceId = targetNodeId,
            TargetId = id,
            Kind = RelationshipKind.Contains,
            SourceFilePath = _filePath,
            SourceLine = line
        });
    }

    /// <summary>
    /// Calculates cyclomatic complexity for a given syntax node.
    /// Counts decision points: if, while, for, foreach, case, catch, &&, ||, ??
    /// </summary>
    private static int CalculateCyclomaticComplexity(SyntaxNode node)
    {
        var complexity = 1; // Base complexity

        foreach (var descendant in node.DescendantNodes())
        {
            switch (descendant.Kind())
            {
                // Conditional statements
                case SyntaxKind.IfStatement:
                case SyntaxKind.ElseClause when descendant.Parent is IfStatementSyntax { Else: not null }:
                case SyntaxKind.ConditionalExpression:
                    complexity++;
                    break;

                // Loop statements
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    complexity++;
                    break;

                // Switch cases
                case SyntaxKind.SwitchSection:
                case SyntaxKind.SwitchExpressionArm:
                    complexity++;
                    break;

                // Exception handling
                case SyntaxKind.CatchClause:
                    complexity++;
                    break;

                // Logical operators (short-circuiting)
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalOrExpression:
                case SyntaxKind.CoalesceExpression:
                    complexity++;
                    break;

                // Null-conditional operators
                case SyntaxKind.ConditionalAccessExpression:
                    complexity++;
                    break;
            }
        }

        return complexity;
    }
}
