using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class TodoCommentVisitorTests
{
    private const string TestFilePath = "test.cs";

    #region Single Line TODO Comments

    [Test]
    public void Visit_SingleLineTodo_ExtractsTodoNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // TODO: Implement caching
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        var todoNode = todoNodes[0];
        Assert.Multiple(() =>
        {
            Assert.That(todoNode.Kind, Is.EqualTo(DeclarationKind.TodoComment));
            Assert.That(todoNode.Name, Does.Contain("TODO"));
            Assert.That(todoNode.Name, Does.Contain("Implement caching"));
            Assert.That(todoNode.FilePath, Is.EqualTo(TestFilePath));
        });
    }

    [Test]
    public void Visit_TodoWithoutColon_ExtractsTodoNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // TODO implement this later
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        var metadata = GetMetadata(todoNodes[0]);
        Assert.That(metadata?.Text, Is.EqualTo("implement this later"));
    }

    [Test]
    public void Visit_TodoCaseInsensitive_ExtractsAllVariants()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // TODO: uppercase
                                    // todo: lowercase
                                    // Todo: mixed case
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(3));
    }

    #endregion

    #region Different Comment Types

    [Test]
    public void Visit_FixmeComment_ExtractsWithCorrectType()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                // FIXME: This is broken
                                public class MyClass { }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        var metadata = GetMetadata(todoNodes[0]);
        Assert.Multiple(() =>
        {
            Assert.That(metadata?.CommentType, Is.EqualTo("FIXME"));
            Assert.That(metadata?.Text, Is.EqualTo("This is broken"));
        });
    }

    [Test]
    public void Visit_HackComment_ExtractsWithCorrectType()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // HACK: Temporary workaround for issue #123
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        var metadata = GetMetadata(todoNodes[0]);
        Assert.That(metadata?.CommentType, Is.EqualTo("HACK"));
    }

    [Test]
    public void Visit_XxxComment_ExtractsWithCorrectType()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // XXX: Needs review
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        var metadata = GetMetadata(todoNodes[0]);
        Assert.That(metadata?.CommentType, Is.EqualTo("XXX"));
    }

    [Test]
    public void Visit_AllCommentTypes_ExtractsAll()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // TODO: First task
                                    // FIXME: Second task
                                    // HACK: Third task
                                    // XXX: Fourth task
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(4));
        var commentTypes = todoNodes.Select(n => GetMetadata(n)?.CommentType).ToList();
        Assert.That(commentTypes, Is.EquivalentTo(new[] { "TODO", "FIXME", "HACK", "XXX" }));
    }

    #endregion

    #region Containment Edges

    [Test]
    public void Visit_TodoInMethod_LinksToEnclosingMethod()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod()
                                    {
                                        // TODO: Add validation
                                    }
                                }
                            }
                            """;

        var (todoNodes, edges) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        Assert.That(edges, Has.Count.EqualTo(1));

        var edge = edges[0];
        Assert.Multiple(() =>
        {
            Assert.That(edge.SourceId, Is.EqualTo("TestNamespace.MyClass.MyMethod()"));
            Assert.That(edge.TargetId, Is.EqualTo(todoNodes[0].Id));
            Assert.That(edge.Kind, Is.EqualTo(RelationshipKind.Contains));
        });
    }

    [Test]
    public void Visit_TodoInClass_LinksToEnclosingClass()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // TODO: Add more fields
                                    private int _value;
                                }
                            }
                            """;

        var (todoNodes, edges) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        Assert.That(edges, Has.Count.EqualTo(1));

        var edge = edges[0];
        Assert.That(edge.SourceId, Is.EqualTo("TestNamespace.MyClass"));
    }

    [Test]
    public void Visit_TodoBeforeClass_LinksToClass()
    {
        // Note: Comments immediately before a declaration are leading trivia for that declaration,
        // so they associate with the following declaration rather than the enclosing namespace.
        const string code = """
                            namespace TestNamespace
                            {
                                // TODO: Organize this namespace better
                                public class MyClass { }
                            }
                            """;

        var (todoNodes, edges) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        Assert.That(edges, Has.Count.EqualTo(1));

        var edge = edges[0];
        Assert.That(edge.SourceId, Is.EqualTo("TestNamespace.MyClass"));
    }

    #endregion

    #region ID Generation

    [Test]
    public void Visit_TodoNode_HasCorrectIdFormat()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod()
                                    {
                                        // TODO: Test ID format
                                    }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        var todoNode = todoNodes[0];
        Assert.That(todoNode.Id, Does.StartWith("TestNamespace.MyClass.MyMethod()$TODO#"));
    }

    [Test]
    public void Visit_MultipleTodosInSameMethod_HaveUniqueIds()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod()
                                    {
                                        // TODO: First
                                        // TODO: Second
                                        // TODO: Third
                                    }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(3));
        var ids = todoNodes.Select(n => n.Id).Distinct().ToList();
        Assert.That(ids, Has.Count.EqualTo(3), "All TODO nodes should have unique IDs");
    }

    #endregion

    #region Location Tracking

    [Test]
    public void Visit_TodoComment_CapturesCorrectLine()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    // TODO: Line 5 comment
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        var todoNode = todoNodes[0];
        Assert.That(todoNode.StartLine, Is.EqualTo(5));
    }

    [Test]
    public void Visit_TodoComment_CapturesFilePath()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                // TODO: Check file path
                                public class MyClass { }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes[0].FilePath, Is.EqualTo(TestFilePath));
    }

    #endregion

    #region Multi-line Comments

    [Test]
    public void Visit_MultiLineCommentWithTodo_ExtractsTodo()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    /* TODO: Multi-line comment task */
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Has.Count.EqualTo(1));
        var metadata = GetMetadata(todoNodes[0]);
        Assert.That(metadata?.CommentType, Is.EqualTo("TODO"));
    }

    #endregion

    #region Long Content Handling

    [Test]
    public void Visit_LongTodoContent_TruncatesNameButPreservesFullTextInMetadata()
    {
        const string longText = "This is a very long TODO comment that exceeds fifty characters and should be truncated in the name";
        var code = $$"""
                     namespace TestNamespace
                     {
                         public class MyClass
                         {
                             // TODO: {{longText}}
                             public void MyMethod() { }
                         }
                     }
                     """;

        var (todoNodes, _) = AnalyzeTodos(code);

        var todoNode = todoNodes[0];
        Assert.Multiple(() =>
        {
            Assert.That(todoNode.Name.Length, Is.LessThan(longText.Length + 10)); // Name is truncated
            Assert.That(todoNode.Name, Does.EndWith("..."));
            var metadata = GetMetadata(todoNode);
            Assert.That(metadata?.Text, Is.EqualTo(longText)); // Full text preserved in metadata
        });
    }

    #endregion

    #region No Match Cases

    [Test]
    public void Visit_RegularComment_DoesNotExtract()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                // This is just a regular comment
                                public class MyClass { }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Is.Empty);
    }

    [Test]
    public void Visit_XmlDocComment_DoesNotExtract()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                /// <summary>
                                /// This is XML documentation
                                /// </summary>
                                public class MyClass { }
                            }
                            """;

        var (todoNodes, _) = AnalyzeTodos(code);

        Assert.That(todoNodes, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private static (List<DeclarationNode> TodoNodes, List<RelationshipEdge> Edges) AnalyzeTodos(string code)
    {
        var (tree, semanticModel, symbolToNodeId) = CreateCompilationWithSymbolMap(code);
        var visitor = new TodoCommentVisitor(semanticModel, TestFilePath, symbolToNodeId);
        visitor.Visit(tree.GetRoot());
        return (visitor.TodoNodes, visitor.ContainmentEdges);
    }

    private static (SyntaxTree Tree, SemanticModel SemanticModel, Dictionary<ISymbol, string> SymbolToNodeId)
        CreateCompilationWithSymbolMap(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);

        // First run DeclarationVisitor to build symbol-to-node-id mapping
        var declarationVisitor = new DeclarationVisitor(semanticModel, TestFilePath);
        declarationVisitor.Visit(tree.GetRoot());

        return (tree, semanticModel, declarationVisitor.SymbolToNodeId);
    }

    private static TodoMetadata? GetMetadata(DeclarationNode node)
    {
        if (string.IsNullOrEmpty(node.Metadata)) return null;
        return JsonSerializer.Deserialize<TodoMetadata>(node.Metadata);
    }

    private sealed record TodoMetadata(
        [property: JsonPropertyName("commentType")] string CommentType,
        [property: JsonPropertyName("text")] string Text);

    #endregion
}
