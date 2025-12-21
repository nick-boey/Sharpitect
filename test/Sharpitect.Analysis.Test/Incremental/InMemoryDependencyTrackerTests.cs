using Sharpitect.Analysis.Incremental;

namespace Sharpitect.Analysis.Test.Incremental;

[TestFixture]
public class InMemoryDependencyTrackerTests
{
    private InMemoryDependencyTracker _tracker = null!;

    [SetUp]
    public void SetUp()
    {
        _tracker = new InMemoryDependencyTracker();
    }

    #region RecordReference Tests

    [Test]
    public void RecordReference_ShouldTrackDependency()
    {
        _tracker.RecordReference("file1.cs", "MyNamespace.MyClass");

        var dependentFiles = _tracker.GetDependentFiles("MyNamespace.MyClass");

        Assert.That(dependentFiles, Does.Contain("file1.cs"));
    }

    [Test]
    public void RecordReference_MultipleDependencies_ShouldTrackAll()
    {
        _tracker.RecordReference("file1.cs", "MyNamespace.MyClass");
        _tracker.RecordReference("file2.cs", "MyNamespace.MyClass");
        _tracker.RecordReference("file3.cs", "MyNamespace.MyClass");

        var dependentFiles = _tracker.GetDependentFiles("MyNamespace.MyClass");

        Assert.Multiple(() =>
        {
            Assert.That(dependentFiles, Has.Count.EqualTo(3));
            Assert.That(dependentFiles, Does.Contain("file1.cs"));
            Assert.That(dependentFiles, Does.Contain("file2.cs"));
            Assert.That(dependentFiles, Does.Contain("file3.cs"));
        });
    }

    [Test]
    public void RecordReference_SameFileTwice_ShouldNotDuplicate()
    {
        _tracker.RecordReference("file1.cs", "MyNamespace.MyClass");
        _tracker.RecordReference("file1.cs", "MyNamespace.MyClass");

        var dependentFiles = _tracker.GetDependentFiles("MyNamespace.MyClass");

        Assert.That(dependentFiles, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecordReference_FileReferencesMultipleNodes_ShouldTrackAll()
    {
        _tracker.RecordReference("file1.cs", "MyNamespace.ClassA");
        _tracker.RecordReference("file1.cs", "MyNamespace.ClassB");

        var referencedNodes = _tracker.GetReferencedNodes("file1.cs");

        Assert.Multiple(() =>
        {
            Assert.That(referencedNodes, Has.Count.EqualTo(2));
            Assert.That(referencedNodes, Does.Contain("MyNamespace.ClassA"));
            Assert.That(referencedNodes, Does.Contain("MyNamespace.ClassB"));
        });
    }

    #endregion

    #region GetDependentFiles Tests

    [Test]
    public void GetDependentFiles_NonExistentNode_ShouldReturnEmpty()
    {
        var dependentFiles = _tracker.GetDependentFiles("NonExistent.Class");

        Assert.That(dependentFiles, Is.Empty);
    }

    [Test]
    public void GetDependentFiles_ShouldReturnFilesReferencingNode()
    {
        _tracker.RecordReference("file1.cs", "NodeA");
        _tracker.RecordReference("file2.cs", "NodeA");
        _tracker.RecordReference("file3.cs", "NodeB");

        var dependentFiles = _tracker.GetDependentFiles("NodeA");

        Assert.Multiple(() =>
        {
            Assert.That(dependentFiles, Has.Count.EqualTo(2));
            Assert.That(dependentFiles, Does.Not.Contain("file3.cs"));
        });
    }

    #endregion

    #region GetDependentFilesForNodes Tests

    [Test]
    public void GetDependentFilesForNodes_ShouldReturnUnionOfDependentFiles()
    {
        _tracker.RecordReference("file1.cs", "NodeA");
        _tracker.RecordReference("file2.cs", "NodeB");
        _tracker.RecordReference("file3.cs", "NodeC");

        var dependentFiles = _tracker.GetDependentFilesForNodes(["NodeA", "NodeB"]);

        Assert.Multiple(() =>
        {
            Assert.That(dependentFiles, Has.Count.EqualTo(2));
            Assert.That(dependentFiles, Does.Contain("file1.cs"));
            Assert.That(dependentFiles, Does.Contain("file2.cs"));
            Assert.That(dependentFiles, Does.Not.Contain("file3.cs"));
        });
    }

    [Test]
    public void GetDependentFilesForNodes_WithOverlappingFiles_ShouldNotDuplicate()
    {
        _tracker.RecordReference("file1.cs", "NodeA");
        _tracker.RecordReference("file1.cs", "NodeB");
        _tracker.RecordReference("file2.cs", "NodeA");

        var dependentFiles = _tracker.GetDependentFilesForNodes(["NodeA", "NodeB"]);

        Assert.Multiple(() =>
        {
            Assert.That(dependentFiles, Has.Count.EqualTo(2));
            Assert.That(dependentFiles, Does.Contain("file1.cs"));
            Assert.That(dependentFiles, Does.Contain("file2.cs"));
        });
    }

    [Test]
    public void GetDependentFilesForNodes_EmptyInput_ShouldReturnEmpty()
    {
        _tracker.RecordReference("file1.cs", "NodeA");

        var dependentFiles = _tracker.GetDependentFilesForNodes([]);

        Assert.That(dependentFiles, Is.Empty);
    }

    #endregion

    #region GetReferencedNodes Tests

    [Test]
    public void GetReferencedNodes_NonExistentFile_ShouldReturnEmpty()
    {
        var referencedNodes = _tracker.GetReferencedNodes("nonexistent.cs");

        Assert.That(referencedNodes, Is.Empty);
    }

    #endregion

    #region RemoveReferencesFromFile Tests

    [Test]
    public void RemoveReferencesFromFile_ShouldClearFileDependencies()
    {
        _tracker.RecordReference("file1.cs", "NodeA");
        _tracker.RecordReference("file1.cs", "NodeB");
        _tracker.RecordReference("file2.cs", "NodeA");

        _tracker.RemoveReferencesFromFile("file1.cs");

        Assert.Multiple(() =>
        {
            // file1.cs no longer references NodeA
            Assert.That(_tracker.GetDependentFiles("NodeA"), Does.Not.Contain("file1.cs"));
            // file2.cs still references NodeA
            Assert.That(_tracker.GetDependentFiles("NodeA"), Does.Contain("file2.cs"));
            // file1.cs has no references
            Assert.That(_tracker.GetReferencedNodes("file1.cs"), Is.Empty);
        });
    }

    [Test]
    public void RemoveReferencesFromFile_NonExistentFile_ShouldNotThrow()
    {
        Assert.DoesNotThrow(() => _tracker.RemoveReferencesFromFile("nonexistent.cs"));
    }

    [Test]
    public void RemoveReferencesFromFile_ShouldRemoveNodeFromDependentsWhenNoMoreReferences()
    {
        _tracker.RecordReference("file1.cs", "NodeA");

        _tracker.RemoveReferencesFromFile("file1.cs");

        Assert.That(_tracker.GetDependentFiles("NodeA"), Is.Empty);
    }

    #endregion

    #region Clear Tests

    [Test]
    public void Clear_ShouldRemoveAllDependencies()
    {
        _tracker.RecordReference("file1.cs", "NodeA");
        _tracker.RecordReference("file2.cs", "NodeB");

        _tracker.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(_tracker.GetDependentFiles("NodeA"), Is.Empty);
            Assert.That(_tracker.GetDependentFiles("NodeB"), Is.Empty);
            Assert.That(_tracker.GetReferencedNodes("file1.cs"), Is.Empty);
            Assert.That(_tracker.GetReferencedNodes("file2.cs"), Is.Empty);
        });
    }

    #endregion
}
