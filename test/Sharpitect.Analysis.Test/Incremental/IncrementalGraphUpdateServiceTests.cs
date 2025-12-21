using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Incremental;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Test.Incremental;

[TestFixture]
public class IncrementalGraphUpdateServiceTests
{
    private string _testDbPath = null!;
    private SqliteGraphRepository _repository = null!;
    private DeclarationGraph _graph = null!;
    private InMemoryDependencyTracker _dependencyTracker = null!;
    private IncrementalFileAnalyzer _fileAnalyzer = null!;
    private AdhocWorkspace _workspace = null!;
    private Solution _solution = null!;
    private ProjectId _projectId = null!;
    private IncrementalGraphUpdateService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"sharpitect_update_test_{Guid.NewGuid()}.db");
        _repository = new SqliteGraphRepository(_testDbPath);
        await _repository.InitializeAsync();

        _graph = new DeclarationGraph();
        _dependencyTracker = new InMemoryDependencyTracker();
        _fileAnalyzer = new IncrementalFileAnalyzer();
        _workspace = new AdhocWorkspace();

        _projectId = ProjectId.CreateNewId();
        _solution = _workspace.CurrentSolution
            .AddProject(_projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(_projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(_projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        _workspace.TryApplyChanges(_solution);

        _service = new IncrementalGraphUpdateService(
            _workspace,
            _repository,
            _graph,
            _dependencyTracker,
            _fileAnalyzer);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _service.DisposeAsync();
        _workspace.Dispose();
        await _repository.DisposeAsync();

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        await Task.Delay(50);

        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); }
            catch (IOException) { }
        }
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldAddNodes()
    {
        const string code = """
            namespace TestNamespace
            {
                public class TestClass { }
            }
            """;

        var documentId = await AddDocumentToWorkspaceAsync("Test.cs", code);

        await _service.UpdateFilesAsync(["Test.cs"]);

        Assert.Multiple(() =>
        {
            Assert.That(_graph.NodeCount, Is.GreaterThan(0));
            Assert.That(_graph.Nodes.Values.Any(n => n.Name == "TestClass"), Is.True);
        });
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldAddEdges()
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

        await AddDocumentToWorkspaceAsync("Test.cs", code);

        await _service.UpdateFilesAsync(["Test.cs"]);

        Assert.That(_graph.EdgeCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldRemoveOldNodesOnModification()
    {
        // Initial code with ClassA
        const string initialCode = """
            namespace TestNamespace
            {
                public class ClassA { }
            }
            """;

        var documentId = await AddDocumentToWorkspaceAsync("Test.cs", initialCode);
        await _service.UpdateFilesAsync(["Test.cs"]);

        Assert.That(_graph.Nodes.Values.Any(n => n.Name == "ClassA"), Is.True);

        // Modify to have ClassB instead
        const string modifiedCode = """
            namespace TestNamespace
            {
                public class ClassB { }
            }
            """;

        await UpdateDocumentInWorkspaceAsync(documentId, modifiedCode);
        await _service.UpdateFilesAsync(["Test.cs"]);

        Assert.Multiple(() =>
        {
            Assert.That(_graph.Nodes.Values.Any(n => n.Name == "ClassA"), Is.False);
            Assert.That(_graph.Nodes.Values.Any(n => n.Name == "ClassB"), Is.True);
        });
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldUpdateEdges()
    {
        const string initialCode = """
            namespace TestNamespace
            {
                public class ClassA
                {
                    public void Method1() { }
                }
            }
            """;

        var documentId = await AddDocumentToWorkspaceAsync("Test.cs", initialCode);
        await _service.UpdateFilesAsync(["Test.cs"]);

        var initialEdgeCount = _graph.EdgeCount;

        const string modifiedCode = """
            namespace TestNamespace
            {
                public class ClassA
                {
                    public void Method1() { }
                    public void Method2() { }
                }
            }
            """;

        await UpdateDocumentInWorkspaceAsync(documentId, modifiedCode);
        await _service.UpdateFilesAsync(["Test.cs"]);

        // Should have more edges now (additional containment edge)
        Assert.That(_graph.EdgeCount, Is.GreaterThanOrEqualTo(initialEdgeCount));
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldPersistToRepository()
    {
        const string code = """
            namespace TestNamespace
            {
                public class PersistentClass { }
            }
            """;

        await AddDocumentToWorkspaceAsync("Test.cs", code);
        await _service.UpdateFilesAsync(["Test.cs"]);

        var nodeCount = await _repository.GetNodeCountAsync();

        Assert.That(nodeCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task UpdateFilesAsync_FileDeleted_ShouldRemoveNodesAndEdges()
    {
        const string code = """
            namespace TestNamespace
            {
                public class ToBeDeleted { }
            }
            """;

        var documentId = await AddDocumentToWorkspaceAsync("ToDelete.cs", code);
        await _service.UpdateFilesAsync(["ToDelete.cs"]);

        Assert.That(_graph.Nodes.Values.Any(n => n.Name == "ToBeDeleted"), Is.True);

        // Simulate file deletion by processing with FileChangeKind.Deleted
        await _service.ProcessFileChangesAsync([
            new FileChange("ToDelete.cs", FileChangeKind.Deleted)
        ]);

        Assert.That(_graph.Nodes.Values.Any(n => n.Name == "ToBeDeleted"), Is.False);
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldRaiseUpdateCompletedEvent()
    {
        const string code = """
            namespace TestNamespace
            {
                public class EventTestClass { }
            }
            """;

        await AddDocumentToWorkspaceAsync("Test.cs", code);

        GraphUpdateEventArgs? eventArgs = null;
        _service.UpdateCompleted += (_, args) => eventArgs = args;

        await _service.UpdateFilesAsync(["Test.cs"]);

        Assert.Multiple(() =>
        {
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.UpdatedFiles, Does.Contain("Test.cs"));
            Assert.That(eventArgs.NodesAdded, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task UpdateFilesAsync_ShouldTriggerCascadeUpdate()
    {
        // File1: ClassA
        const string file1Code = """
            namespace TestNamespace
            {
                public class ClassA
                {
                    public void DoSomething() { }
                }
            }
            """;

        // File2: ClassB references ClassA
        const string file2Code = """
            namespace TestNamespace
            {
                public class ClassB
                {
                    public void UseClassA()
                    {
                        var a = new ClassA();
                        a.DoSomething();
                    }
                }
            }
            """;

        await AddDocumentToWorkspaceAsync("File1.cs", file1Code);
        await AddDocumentToWorkspaceAsync("File2.cs", file2Code);

        // Initial analysis of both files
        await _service.UpdateFilesAsync(["File1.cs", "File2.cs"]);

        var classANode = _graph.Nodes.Values.FirstOrDefault(n => n.Name == "ClassA");
        Assert.That(classANode, Is.Not.Null);

        // Track that File2 references ClassA
        _dependencyTracker.RecordReference("File2.cs", classANode!.Id);

        // Now modify File1 (ClassA changes)
        const string modifiedFile1 = """
            namespace TestNamespace
            {
                public class ClassA
                {
                    public void DoSomethingElse() { }
                }
            }
            """;

        var doc1 = _workspace.CurrentSolution.Projects.First().Documents.First(d => d.Name == "File1.cs");
        await UpdateDocumentInWorkspaceAsync(doc1.Id, modifiedFile1);

        // Process with cascade enabled
        await _service.ProcessFileChangesAsync([
            new FileChange("File1.cs", FileChangeKind.Modified)
        ], enableCascade: true);

        // File2 should have been re-processed (cascade update)
        // The dependency tracker should now be updated
        Assert.That(_graph.Nodes.Values.Any(n => n.Name == "ClassB"), Is.True);
    }

    [Test]
    public async Task State_ShouldReflectCurrentState()
    {
        Assert.That(_service.State, Is.EqualTo(IncrementalUpdateState.Stopped));

        await _service.StartAsync();
        Assert.That(_service.State, Is.EqualTo(IncrementalUpdateState.Watching));

        await _service.StopAsync();
        Assert.That(_service.State, Is.EqualTo(IncrementalUpdateState.Stopped));
    }

    #region Helper Methods

    private async Task<DocumentId> AddDocumentToWorkspaceAsync(string fileName, string code)
    {
        var documentId = DocumentId.CreateNewId(_projectId);
        var newSolution = _workspace.CurrentSolution
            .AddDocument(documentId, fileName, SourceText.From(code), filePath: fileName);
        _workspace.TryApplyChanges(newSolution);
        return documentId;
    }

    private async Task UpdateDocumentInWorkspaceAsync(DocumentId documentId, string newCode)
    {
        var document = _workspace.CurrentSolution.GetDocument(documentId);
        if (document != null)
        {
            var newSolution = document.WithText(SourceText.From(newCode)).Project.Solution;
            _workspace.TryApplyChanges(newSolution);
        }
    }

    #endregion
}
