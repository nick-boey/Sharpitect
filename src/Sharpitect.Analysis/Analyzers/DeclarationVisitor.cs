using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Walks the syntax tree to extract declaration nodes and containment edges.
/// </summary>
public sealed class DeclarationVisitor : CSharpSyntaxWalker
{
    private readonly bool _visitLocals;
    private readonly SemanticModel _semanticModel;
    private readonly string _filePath;
    private readonly Stack<string> _containerStack = new();


    /// <summary>
    /// Gets all discovered declaration nodes.
    /// </summary>
    public List<DeclarationNode> Nodes { get; } = [];

    /// <summary>
    /// Gets all containment edges discovered during traversal.
    /// </summary>
    public List<RelationshipEdge> ContainmentEdges { get; } = [];

    /// <summary>
    /// Gets a lookup from Roslyn symbols to node IDs.
    /// </summary>
    public Dictionary<ISymbol, string> SymbolToNodeId { get; } = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Creates a new declaration visitor.
    /// </summary>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="filePath">The file path being analyzed.</param>
    /// <param name="visitLocals">True to visit all local declarations. Defaults as false to avoid duplicate id's.</param>
    public DeclarationVisitor(SemanticModel semanticModel, string filePath, bool visitLocals = false)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
        _visitLocals = visitLocals;
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Namespace);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitFileScopedNamespaceDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitFileScopedNamespaceDeclaration(node);
        }
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Namespace);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitNamespaceDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitNamespaceDeclaration(node);
        }
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateTypeNode(symbol, node, DeclarationKind.Class);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitClassDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitClassDeclaration(node);
        }
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateTypeNode(symbol, node, DeclarationKind.Interface);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitInterfaceDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitInterfaceDeclaration(node);
        }
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateTypeNode(symbol, node, DeclarationKind.Struct);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitStructDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitStructDeclaration(node);
        }
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateTypeNode(symbol, node, DeclarationKind.Record);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitRecordDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitRecordDeclaration(node);
        }
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNodeWithC4(symbol, node, DeclarationKind.Enum);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitEnumDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitEnumDeclaration(node);
        }
    }

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Delegate);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitDelegateDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Method);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitMethodDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitMethodDeclaration(node);
        }
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Constructor);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitConstructorDeclaration(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitConstructorDeclaration(node);
        }
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Property);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitPropertyDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(variable);
            if (symbol != null)
            {
                var declNode = CreateNode(symbol, variable, DeclarationKind.Field);
                Nodes.Add(declNode);
                AddContainmentEdge(declNode.Id);
            }
        }

        base.VisitFieldDeclaration(node);
    }

    public override void VisitEventDeclaration(EventDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Event);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitEventDeclaration(node);
    }

    public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(variable);
            if (symbol != null)
            {
                var declNode = CreateNode(symbol, variable, DeclarationKind.Event);
                Nodes.Add(declNode);
                AddContainmentEdge(declNode.Id);
            }
        }

        base.VisitEventFieldDeclaration(node);
    }

    public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Indexer);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitIndexerDeclaration(node);
    }

    public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.EnumMember);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitEnumMemberDeclaration(node);
    }

    public override void VisitParameter(ParameterSyntax node)
    {
        if (!_visitLocals) return;

        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.Parameter);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitParameter(node);
    }

    public override void VisitTypeParameter(TypeParameterSyntax node)
    {
        if (!_visitLocals) return;

        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.TypeParameter);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);
        }

        base.VisitTypeParameter(node);
    }

    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        if (!_visitLocals) return;

        // Only track local variables (not field declarations, which are handled separately)
        if (node.Parent?.Parent is LocalDeclarationStatementSyntax)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol != null)
            {
                var declNode = CreateNode(symbol, node, DeclarationKind.LocalVariable);
                Nodes.Add(declNode);
                AddContainmentEdge(declNode.Id);
            }
        }

        base.VisitVariableDeclarator(node);
    }

    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        if (!_visitLocals) return;

        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null)
        {
            var declNode = CreateNode(symbol, node, DeclarationKind.LocalFunction);
            Nodes.Add(declNode);
            AddContainmentEdge(declNode.Id);

            _containerStack.Push(declNode.Id);
            base.VisitLocalFunctionStatement(node);
            _containerStack.Pop();
        }
        else
        {
            base.VisitLocalFunctionStatement(node);
        }
    }

    private DeclarationNode CreateNode(ISymbol symbol, SyntaxNode node, DeclarationKind kind)
    {
        var location = node.GetLocation().GetLineSpan();
        var id = symbol.ToDisplayString();

        SymbolToNodeId[symbol] = id;

        return new DeclarationNode
        {
            Id = id,
            Name = symbol.Name,
            Kind = kind,
            FilePath = _filePath,
            StartLine = location.StartLinePosition.Line + 1,
            StartColumn = location.StartLinePosition.Character + 1,
            EndLine = location.EndLinePosition.Line + 1,
            EndColumn = location.EndLinePosition.Character + 1
        };
    }

    private DeclarationNode CreateNodeWithC4(INamedTypeSymbol symbol, SyntaxNode node, DeclarationKind kind)
    {
        var location = node.GetLocation().GetLineSpan();
        var id = symbol.ToDisplayString();

        SymbolToNodeId[symbol] = id;

        // Extract C4 Component annotation if present
        var (c4Level, c4Description) = ExtractC4Annotation(symbol);

        return new DeclarationNode
        {
            Id = id,
            Name = symbol.Name,
            Kind = kind,
            FilePath = _filePath,
            StartLine = location.StartLinePosition.Line + 1,
            StartColumn = location.StartLinePosition.Character + 1,
            EndLine = location.EndLinePosition.Line + 1,
            EndColumn = location.EndLinePosition.Character + 1,
            C4Level = c4Level,
            C4Description = c4Description
        };
    }

    private DeclarationNode CreateTypeNode(INamedTypeSymbol symbol, TypeDeclarationSyntax node, DeclarationKind kind)
    {
        var location = node.GetLocation().GetLineSpan();
        var id = symbol.ToDisplayString();

        SymbolToNodeId[symbol] = id;

        // Extract C4 Component annotation if present
        var (c4Level, c4Description) = ExtractC4Annotation(symbol);

        return new DeclarationNode
        {
            Id = id,
            Name = symbol.Name,
            Kind = kind,
            FilePath = _filePath,
            StartLine = location.StartLinePosition.Line + 1,
            StartColumn = location.StartLinePosition.Character + 1,
            EndLine = location.EndLinePosition.Line + 1,
            EndColumn = location.EndLinePosition.Character + 1,
            C4Level = c4Level,
            C4Description = c4Description
        };
    }

    private static (C4Level level, string? description) ExtractC4Annotation(INamedTypeSymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name is not ("ComponentAttribute" or "Component")) continue;
            string? description = null;

            // Get description from named argument
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg is { Key: "Description", Value.Value: string desc })
                {
                    description = desc;
                }
            }

            return (C4Level.Component, description);
        }

        return (C4Level.None, null);
    }

    private void AddContainmentEdge(string childId)
    {
        if (_containerStack.Count <= 0) return;
        var parentId = _containerStack.Peek();
        ContainmentEdges.Add(new RelationshipEdge
        {
            Id = Guid.NewGuid().ToString(),
            SourceId = parentId,
            TargetId = childId,
            Kind = RelationshipKind.Contains,
            SourceFilePath = _filePath
        });
    }
}