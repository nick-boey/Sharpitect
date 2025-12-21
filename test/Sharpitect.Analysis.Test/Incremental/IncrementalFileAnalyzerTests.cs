using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Incremental;

namespace Sharpitect.Analysis.Test.Incremental;

[TestFixture]
public class IncrementalFileAnalyzerTests
{
    private IncrementalFileAnalyzer _analyzer = null!;

    [SetUp]
    public void SetUp()
    {
        _analyzer = new IncrementalFileAnalyzer();
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldExtractNodes()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public void TestMethod() { }
                }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Nodes, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(result.Nodes.Any(n => n.Kind == DeclarationKind.Namespace), Is.True);
            Assert.That(result.Nodes.Any(n => n.Kind == DeclarationKind.Class), Is.True);
            Assert.That(result.Nodes.Any(n => n.Kind == DeclarationKind.Method), Is.True);
        });
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldExtractContainmentEdges()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public void TestMethod() { }
                }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        // Should have containment edges: Namespace->Class, Class->Method
        Assert.Multiple(() =>
        {
            Assert.That(result.Edges.Count(e => e.Kind == RelationshipKind.Contains), Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldExtractReferenceEdges()
    {
        const string code = """
            namespace TestNamespace
            {
                public class ClassA
                {
                    public void CallB()
                    {
                        var b = new ClassB();
                        b.DoSomething();
                    }
                }

                public class ClassB
                {
                    public void DoSomething() { }
                }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        // Should have Calls edge from CallB to DoSomething
        Assert.That(result.Edges.Any(e => e.Kind == RelationshipKind.Calls), Is.True);
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldReturnSymbolMappings()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass { }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.SymbolMappings, Is.Not.Empty);
            Assert.That(result.SymbolMappings.ContainsKey("TestNamespace.TestClass"), Is.True);
        });
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldResolveExternalReferences()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public void CallExternal()
                    {
                        var ext = new ExternalClass();
                        ext.Method();
                    }
                }
            }
            """;

        // Provide existing mappings for ExternalClass
        var existingMappings = new Dictionary<string, string>
        {
            ["ExternalNamespace.ExternalClass"] = "ExternalNamespace.ExternalClass",
            ["ExternalNamespace.ExternalClass.Method()"] = "ExternalNamespace.ExternalClass.Method()"
        };
        var existingNodeIds = new HashSet<string>(existingMappings.Values);

        var result = await AnalyzeCodeAsync(code, existingMappings, existingNodeIds);

        // Should still extract nodes from this file
        Assert.That(result.Nodes.Any(n => n.Name == "TestClass"), Is.True);
    }

    [Test]
    public async Task AnalyzeFileAsync_WithCompilationErrors_ShouldExtractValidDeclarations()
    {
        const string code = """
            namespace TestNamespace
            {
                public class ValidClass
                {
                    public void ValidMethod() { }
                }

                public class ClassWithError
                {
                    public UndefinedType BrokenMethod() { }
                }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        // Should still extract ValidClass and ValidMethod despite errors
        Assert.Multiple(() =>
        {
            Assert.That(result.Nodes.Any(n => n.Name == "ValidClass"), Is.True);
            Assert.That(result.Nodes.Any(n => n.Name == "ValidMethod"), Is.True);
            Assert.That(result.Nodes.Any(n => n.Name == "ClassWithError"), Is.True);
        });
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldTrackFilePathOnNodes()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass { }
            }
            """;

        var result = await AnalyzeCodeAsync(code, filePath: "/test/path/File.cs");

        Assert.That(result.Nodes.All(n => n.FilePath == "/test/path/File.cs"), Is.True);
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldTrackSourceFileOnEdges()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public void TestMethod() { }
                }
            }
            """;

        var result = await AnalyzeCodeAsync(code, filePath: "/test/path/File.cs");

        Assert.That(result.Edges.All(e => e.SourceFilePath == "/test/path/File.cs"), Is.True);
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldExtractInheritance()
    {
        const string code = """
            namespace TestNamespace
            {
                public class BaseClass { }
                public class DerivedClass : BaseClass { }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        Assert.That(result.Edges.Any(e => e.Kind == RelationshipKind.Inherits), Is.True);
    }

    [Test]
    public async Task AnalyzeFileAsync_ShouldExtractInterfaceImplementation()
    {
        const string code = """
            namespace TestNamespace
            {
                public interface IService { }
                public class ServiceImpl : IService { }
            }
            """;

        var result = await AnalyzeCodeAsync(code);

        Assert.That(result.Edges.Any(e => e.Kind == RelationshipKind.Implements), Is.True);
    }

    #region Helper Methods

    private async Task<FileAnalysisResult> AnalyzeCodeAsync(
        string code,
        Dictionary<string, string>? existingMappings = null,
        HashSet<string>? existingNodeIds = null,
        string filePath = "test.cs")
    {
        var (document, compilation) = await CreateDocumentAndCompilationAsync(code, filePath);

        return await _analyzer.AnalyzeFileAsync(
            document,
            compilation,
            existingMappings ?? new Dictionary<string, string>(),
            existingNodeIds ?? [],
            visitLocals: false);
    }

    private static async Task<(Document, Compilation)> CreateDocumentAndCompilationAsync(
        string code, string filePath = "test.cs")
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddDocument(documentId, filePath, SourceText.From(code), filePath: filePath);

        workspace.TryApplyChanges(solution);
        var document = workspace.CurrentSolution.GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync() ??
            throw new InvalidOperationException("Failed to get compilation");

        return (document, compilation);
    }

    #endregion
}
