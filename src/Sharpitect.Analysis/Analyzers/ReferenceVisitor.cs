using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Walks the syntax tree to extract reference and call relationships.
/// </summary>
public sealed class ReferenceVisitor : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly string _filePath;
    private readonly Dictionary<ISymbol, string> _symbolToNodeId;
    private readonly HashSet<string> _solutionNodeIds;
    private readonly Stack<string> _currentMemberStack = new();

    /// <summary>
    /// Gets all relationship edges discovered during traversal.
    /// </summary>
    public List<RelationshipEdge> Edges { get; } = [];

    /// <summary>
    /// Creates a new reference visitor.
    /// </summary>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="filePath">The file path being analyzed.</param>
    /// <param name="symbolToNodeId">Mapping from symbols to node IDs.</param>
    /// <param name="solutionNodeIds">Set of all node IDs in the solution (for filtering external references).</param>
    public ReferenceVisitor(
        SemanticModel semanticModel,
        string filePath,
        Dictionary<ISymbol, string> symbolToNodeId,
        HashSet<string> solutionNodeIds)
    {
        _semanticModel = semanticModel;
        _filePath = filePath;
        _symbolToNodeId = symbolToNodeId;
        _solutionNodeIds = solutionNodeIds;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var classId))
        {
            // Handle inheritance
            if (symbol.BaseType != null && !IsSystemObject(symbol.BaseType))
            {
                AddEdgeIfInSolution(classId, symbol.BaseType, RelationshipKind.Inherits, node);
            }

            // Handle interface implementations
            foreach (var iface in symbol.Interfaces)
            {
                AddEdgeIfInSolution(classId, iface, RelationshipKind.Implements, node);
            }
        }

        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var structId))
        {
            // Handle interface implementations
            foreach (var iface in symbol.Interfaces)
            {
                AddEdgeIfInSolution(structId, iface, RelationshipKind.Implements, node);
            }
        }

        base.VisitStructDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var recordId))
        {
            // Handle inheritance
            if (symbol.BaseType != null && !IsSystemObject(symbol.BaseType) && !IsSystemValueType(symbol.BaseType))
            {
                AddEdgeIfInSolution(recordId, symbol.BaseType, RelationshipKind.Inherits, node);
            }

            // Handle interface implementations
            foreach (var iface in symbol.Interfaces)
            {
                AddEdgeIfInSolution(recordId, iface, RelationshipKind.Implements, node);
            }
        }

        base.VisitRecordDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var methodId))
        {
            _currentMemberStack.Push(methodId);

            // Handle override relationships
            if (symbol.IsOverride && symbol.OverriddenMethod != null)
            {
                AddEdgeIfInSolution(methodId, symbol.OverriddenMethod, RelationshipKind.Overrides, node);
            }

            // Handle return type reference
            if (symbol.ReturnType != null && !symbol.ReturnsVoid)
            {
                AddTypeReferenceEdge(methodId, symbol.ReturnType, node);
            }

            // Handle parameter type references
            foreach (var param in symbol.Parameters)
            {
                AddTypeReferenceEdge(methodId, param.Type, node);
            }

            base.VisitMethodDeclaration(node);
            _currentMemberStack.Pop();
        }
        else
        {
            base.VisitMethodDeclaration(node);
        }
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var ctorId))
        {
            _currentMemberStack.Push(ctorId);

            // Handle parameter type references
            foreach (var param in symbol.Parameters)
            {
                AddTypeReferenceEdge(ctorId, param.Type, node);
            }

            base.VisitConstructorDeclaration(node);
            _currentMemberStack.Pop();
        }
        else
        {
            base.VisitConstructorDeclaration(node);
        }
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var propId))
        {
            _currentMemberStack.Push(propId);

            // Handle property type reference
            AddTypeReferenceEdge(propId, symbol.Type, node);

            base.VisitPropertyDeclaration(node);
            _currentMemberStack.Pop();
        }
        else
        {
            base.VisitPropertyDeclaration(node);
        }
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        foreach (var variable in node.Declaration.Variables)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(variable);
            if (symbol is IFieldSymbol fieldSymbol && TryGetNodeId(fieldSymbol, out var fieldId))
            {
                // Handle field type reference
                AddTypeReferenceEdge(fieldId, fieldSymbol.Type, variable);
            }
        }

        base.VisitFieldDeclaration(node);
    }

    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol != null && TryGetNodeId(symbol, out var localFuncId))
        {
            _currentMemberStack.Push(localFuncId);
            base.VisitLocalFunctionStatement(node);
            _currentMemberStack.Pop();
        }
        else
        {
            base.VisitLocalFunctionStatement(node);
        }
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (_currentMemberStack.Count > 0)
        {
            var currentMemberId = _currentMemberStack.Peek();
            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            var targetSymbol = symbolInfo.Symbol;

            if (targetSymbol is IMethodSymbol methodSymbol)
            {
                AddEdgeIfInSolution(currentMemberId, methodSymbol, RelationshipKind.Calls, node);
            }
        }

        base.VisitInvocationExpression(node);
    }

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (_currentMemberStack.Count > 0)
        {
            var currentMemberId = _currentMemberStack.Peek();
            var symbolInfo = _semanticModel.GetSymbolInfo(node);

            if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
            {
                AddEdgeIfInSolution(currentMemberId, constructorSymbol, RelationshipKind.Constructs, node);
            }
        }

        base.VisitObjectCreationExpression(node);
    }

    public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
    {
        if (_currentMemberStack.Count > 0)
        {
            var currentMemberId = _currentMemberStack.Peek();
            var symbolInfo = _semanticModel.GetSymbolInfo(node);

            if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
            {
                AddEdgeIfInSolution(currentMemberId, constructorSymbol, RelationshipKind.Constructs, node);
            }
        }

        base.VisitImplicitObjectCreationExpression(node);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (_currentMemberStack.Count > 0)
        {
            var currentMemberId = _currentMemberStack.Peek();
            var symbolInfo = _semanticModel.GetSymbolInfo(node);

            if (symbolInfo.Symbol is IPropertySymbol or IFieldSymbol)
            {
                AddEdgeIfInSolution(currentMemberId, symbolInfo.Symbol, RelationshipKind.Uses, node);
            }
        }

        base.VisitMemberAccessExpression(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_currentMemberStack.Count > 0)
        {
            var currentMemberId = _currentMemberStack.Peek();
            var symbolInfo = _semanticModel.GetSymbolInfo(node);

            // Track direct field/property access (not through member access)
            if (symbolInfo.Symbol is IFieldSymbol or IPropertySymbol)
            {
                // Avoid duplicating edges from MemberAccessExpression
                if (node.Parent is not MemberAccessExpressionSyntax memberAccess || memberAccess.Name != node)
                {
                    AddEdgeIfInSolution(currentMemberId, symbolInfo.Symbol, RelationshipKind.Uses, node);
                }
            }
        }

        base.VisitIdentifierName(node);
    }

    private void AddEdgeIfInSolution(string sourceId, ISymbol targetSymbol, RelationshipKind kind, SyntaxNode node)
    {
        // Try direct lookup first
        if (TryGetNodeId(targetSymbol, out var targetId))
        {
            AddEdge(sourceId, targetId, kind, node);
            return;
        }

        // Try original definition (for generic types)
        if (TryGetNodeId(targetSymbol.OriginalDefinition, out targetId))
        {
            AddEdge(sourceId, targetId, kind, node);
            return;
        }

        // For named types, try to find by generated ID
        if (targetSymbol is INamedTypeSymbol namedType)
        {
            var id = namedType.ToDisplayString();
            if (_solutionNodeIds.Contains(id))
            {
                AddEdge(sourceId, id, kind, node);
            }
        }
    }

    private void AddTypeReferenceEdge(string sourceId, ITypeSymbol? typeSymbol, SyntaxNode node)
    {
        if (typeSymbol == null) return;

        // Skip primitive types
        if (typeSymbol.SpecialType != SpecialType.None) return;

        // Handle nullable types
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            var underlyingType = nullable.TypeArguments.FirstOrDefault();
            if (underlyingType != null)
            {
                AddTypeReferenceEdge(sourceId, underlyingType, node);
            }

            return;
        }

        // Handle array types
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            AddTypeReferenceEdge(sourceId, arrayType.ElementType, node);
            return;
        }

        // Handle generic type arguments
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                AddTypeReferenceEdge(sourceId, typeArg, node);
            }
        }

        AddEdgeIfInSolution(sourceId, typeSymbol, RelationshipKind.References, node);
    }

    private void AddEdge(string sourceId, string targetId, RelationshipKind kind, SyntaxNode node)
    {
        var location = node.GetLocation().GetLineSpan();

        Edges.Add(new RelationshipEdge
        {
            Id = Guid.NewGuid().ToString(),
            SourceId = sourceId,
            TargetId = targetId,
            Kind = kind,
            SourceFilePath = _filePath,
            SourceLine = location.StartLinePosition.Line + 1
        });
    }

    private bool TryGetNodeId(ISymbol symbol, out string nodeId)
    {
        if (_symbolToNodeId.TryGetValue(symbol, out nodeId!))
        {
            return true;
        }

        // Try with original definition for generic types
        if (_symbolToNodeId.TryGetValue(symbol.OriginalDefinition, out nodeId!))
        {
            return true;
        }

        nodeId = string.Empty;
        return false;
    }

    private static bool IsSystemObject(INamedTypeSymbol type) =>
        type.SpecialType == SpecialType.System_Object;

    private static bool IsSystemValueType(INamedTypeSymbol type) =>
        type.SpecialType == SpecialType.System_ValueType;
}