using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Extracts TODO, FIXME, HACK, and XXX comments from syntax trivia.
/// </summary>
public sealed partial class TodoCommentVisitor : CSharpSyntaxWalker
{
    private static readonly Regex SingleLinePattern = SingleLineTodoRegex();
    private static readonly Regex MultiLinePattern = MultiLineTodoRegex();

    private readonly string _filePath;
    private readonly SyntaxTree _syntaxTree;
    private readonly Dictionary<ISymbol, string> _symbolToNodeId;
    private readonly SemanticModel _semanticModel;

    /// <summary>
    /// Gets all discovered TODO comment nodes.
    /// </summary>
    public List<DeclarationNode> TodoNodes { get; } = [];

    /// <summary>
    /// Gets all containment edges linking TODOs to their enclosing declarations.
    /// </summary>
    public List<RelationshipEdge> ContainmentEdges { get; } = [];

    /// <summary>
    /// Creates a new TODO comment visitor.
    /// </summary>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="filePath">The file path being analyzed.</param>
    /// <param name="symbolToNodeId">Mapping from symbols to node IDs.</param>
    public TodoCommentVisitor(
        SemanticModel semanticModel,
        string filePath,
        Dictionary<ISymbol, string> symbolToNodeId)
        : base(SyntaxWalkerDepth.Trivia)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
        _syntaxTree = semanticModel.SyntaxTree;
        _symbolToNodeId = symbolToNodeId;
    }

    public override void VisitTrivia(SyntaxTrivia trivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            ProcessSingleLineComment(trivia);
        }
        else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
        {
            ProcessMultiLineComment(trivia);
        }

        base.VisitTrivia(trivia);
    }

    private void ProcessSingleLineComment(SyntaxTrivia trivia)
    {
        var text = trivia.ToFullString();
        var match = SingleLinePattern.Match(text);

        if (!match.Success) return;

        var commentType = match.Groups[1].Value.ToUpperInvariant();
        var content = match.Groups[2].Value.Trim();

        CreateTodoNode(trivia, commentType, content);
    }

    private void ProcessMultiLineComment(SyntaxTrivia trivia)
    {
        var text = trivia.ToFullString();
        var match = MultiLinePattern.Match(text);

        if (!match.Success) return;

        var commentType = match.Groups[1].Value.ToUpperInvariant();
        var content = match.Groups[2].Value.Trim().TrimEnd('*', '/').Trim();

        CreateTodoNode(trivia, commentType, content);
    }

    private void CreateTodoNode(SyntaxTrivia trivia, string commentType, string content)
    {
        var location = _syntaxTree.GetLineSpan(trivia.Span);
        var line = location.StartLinePosition.Line + 1;
        var column = location.StartLinePosition.Character + 1;
        var endColumn = location.EndLinePosition.Character + 1;

        // Find enclosing declaration
        var enclosingNodeId = FindEnclosingDeclaration(trivia);

        var id = enclosingNodeId != null
            ? $"{enclosingNodeId}$TODO#{line}"
            : $"{_filePath}$TODO#{line}";

        // Truncate content for Name if too long
        var displayContent = content.Length > 50 ? content[..50] + "..." : content;
        var name = $"{commentType}: {displayContent}";

        var metadata = JsonSerializer.Serialize(new
        {
            commentType,
            text = content
        });

        var todoNode = new DeclarationNode
        {
            Id = id,
            Name = name,
            Kind = DeclarationKind.TodoComment,
            FilePath = _filePath,
            StartLine = line,
            StartColumn = column,
            EndLine = line,
            EndColumn = endColumn,
            Metadata = metadata
        };

        TodoNodes.Add(todoNode);

        // Add containment edge if we found an enclosing declaration
        if (enclosingNodeId != null)
        {
            ContainmentEdges.Add(new RelationshipEdge
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = enclosingNodeId,
                TargetId = id,
                Kind = RelationshipKind.Contains,
                SourceFilePath = _filePath,
                SourceLine = line
            });
        }
    }

    private string? FindEnclosingDeclaration(SyntaxTrivia trivia)
    {
        // Walk up from trivia to find enclosing declaration node
        var token = trivia.Token;
        var node = token.Parent;

        while (node != null)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol != null && _symbolToNodeId.TryGetValue(symbol, out var nodeId))
            {
                return nodeId;
            }

            node = node.Parent;
        }

        return null;
    }

    [GeneratedRegex(@"^\s*//\s*(TODO|FIXME|HACK|XXX)\s*:?\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SingleLineTodoRegex();

    [GeneratedRegex(@"(TODO|FIXME|HACK|XXX)\s*:?\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MultiLineTodoRegex();
}
