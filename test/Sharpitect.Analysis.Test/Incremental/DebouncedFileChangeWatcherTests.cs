using Sharpitect.Analysis.Incremental;

namespace Sharpitect.Analysis.Test.Incremental;

[TestFixture]
public class DebouncedFileChangeWatcherTests
{
    private string _testDirectory = null!;
    private DebouncedFileChangeWatcher _watcher = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"sharpitect_watcher_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        // Use short debounce for faster tests
        _watcher = new DebouncedFileChangeWatcher(debounceInterval: TimeSpan.FromMilliseconds(100));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _watcher.DisposeAsync();

        // Give a moment for file handles to be released
        await Task.Delay(50);

        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Ignore cleanup failures in tests
            }
        }
    }

    [Test]
    public async Task StartAsync_ShouldStartWatching()
    {
        await _watcher.StartAsync(_testDirectory);

        Assert.That(_watcher.IsWatching, Is.True);
    }

    [Test]
    public async Task StopAsync_ShouldStopWatching()
    {
        await _watcher.StartAsync(_testDirectory);

        await _watcher.StopAsync();

        Assert.That(_watcher.IsWatching, Is.False);
    }

    [Test]
    public async Task FileCreated_ShouldRaiseEvent()
    {
        var receivedChanges = new List<FileChange>();
        var eventReceived = new TaskCompletionSource<bool>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            receivedChanges.AddRange(changes);
            eventReceived.TrySetResult(true);
        };

        await _watcher.StartAsync(_testDirectory);

        // Create a C# file
        var filePath = Path.Combine(_testDirectory, "Test.cs");
        await File.WriteAllTextAsync(filePath, "// test");

        // Wait for event with timeout
        var received = await Task.WhenAny(eventReceived.Task, Task.Delay(2000)) == eventReceived.Task;

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.True, "Event should be received within timeout");
            Assert.That(receivedChanges.Any(c => c.FilePath == filePath), Is.True);
        });
    }

    [Test]
    public async Task FileModified_ShouldRaiseEvent()
    {
        // Create file first
        var filePath = Path.Combine(_testDirectory, "Existing.cs");
        await File.WriteAllTextAsync(filePath, "// initial");

        var receivedChanges = new List<FileChange>();
        var eventReceived = new TaskCompletionSource<bool>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            receivedChanges.AddRange(changes);
            eventReceived.TrySetResult(true);
        };

        await _watcher.StartAsync(_testDirectory);

        // Modify the file
        await File.WriteAllTextAsync(filePath, "// modified");

        // Wait for event with timeout
        var received = await Task.WhenAny(eventReceived.Task, Task.Delay(2000)) == eventReceived.Task;

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.True, "Event should be received within timeout");
            Assert.That(receivedChanges.Any(c => c.FilePath == filePath), Is.True);
        });
    }

    [Test]
    public async Task FileDeleted_ShouldRaiseDeleteEvent()
    {
        // Create file first
        var filePath = Path.Combine(_testDirectory, "ToDelete.cs");
        await File.WriteAllTextAsync(filePath, "// will delete");

        var receivedChanges = new List<FileChange>();
        var eventReceived = new TaskCompletionSource<bool>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            receivedChanges.AddRange(changes);
            eventReceived.TrySetResult(true);
        };

        await _watcher.StartAsync(_testDirectory);

        // Delete the file
        File.Delete(filePath);

        // Wait for event with timeout
        var received = await Task.WhenAny(eventReceived.Task, Task.Delay(2000)) == eventReceived.Task;

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.True, "Event should be received within timeout");
            Assert.That(receivedChanges.Any(c => c.FilePath == filePath && c.Kind == FileChangeKind.Deleted), Is.True);
        });
    }

    [Test]
    public async Task NonCsFile_ShouldBeIgnored()
    {
        var receivedChanges = new List<FileChange>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            receivedChanges.AddRange(changes);
        };

        await _watcher.StartAsync(_testDirectory);

        // Create a non-C# file
        var filePath = Path.Combine(_testDirectory, "Test.txt");
        await File.WriteAllTextAsync(filePath, "test");

        // Wait a bit longer than debounce interval
        await Task.Delay(300);

        Assert.That(receivedChanges, Is.Empty);
    }

    [Test]
    public async Task MultipleRapidChanges_ShouldDebounce()
    {
        var eventCount = 0;
        var lastChanges = new List<FileChange>();
        var eventReceived = new TaskCompletionSource<bool>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            eventCount++;
            lastChanges.Clear();
            lastChanges.AddRange(changes);
            eventReceived.TrySetResult(true);
        };

        await _watcher.StartAsync(_testDirectory);

        // Create a file and modify it rapidly
        var filePath = Path.Combine(_testDirectory, "Rapid.cs");
        await File.WriteAllTextAsync(filePath, "// v1");
        await Task.Delay(20);
        await File.WriteAllTextAsync(filePath, "// v2");
        await Task.Delay(20);
        await File.WriteAllTextAsync(filePath, "// v3");

        // Wait for event with timeout
        var received = await Task.WhenAny(eventReceived.Task, Task.Delay(2000)) == eventReceived.Task;

        // Wait a bit more to ensure no more events
        await Task.Delay(300);

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.True, "Event should be received within timeout");
            // Should only have received one event (debounced)
            Assert.That(eventCount, Is.EqualTo(1), "Should only receive one debounced event");
        });
    }

    [Test]
    public async Task FileInSubdirectory_ShouldBeDetected()
    {
        // Create subdirectory
        var subDir = Path.Combine(_testDirectory, "SubDir");
        Directory.CreateDirectory(subDir);

        var receivedChanges = new List<FileChange>();
        var eventReceived = new TaskCompletionSource<bool>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            receivedChanges.AddRange(changes);
            eventReceived.TrySetResult(true);
        };

        await _watcher.StartAsync(_testDirectory);

        // Create a file in subdirectory
        var filePath = Path.Combine(subDir, "Nested.cs");
        await File.WriteAllTextAsync(filePath, "// nested");

        // Wait for event with timeout
        var received = await Task.WhenAny(eventReceived.Task, Task.Delay(2000)) == eventReceived.Task;

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.True, "Event should be received within timeout");
            Assert.That(receivedChanges.Any(c => c.FilePath == filePath), Is.True);
        });
    }

    [Test]
    public async Task FileRenamed_ShouldRaiseRenameEvent()
    {
        // Create file first
        var oldPath = Path.Combine(_testDirectory, "OldName.cs");
        var newPath = Path.Combine(_testDirectory, "NewName.cs");
        await File.WriteAllTextAsync(oldPath, "// will rename");

        var receivedChanges = new List<FileChange>();
        var eventReceived = new TaskCompletionSource<bool>();

        _watcher.ChangesDetected += (_, changes) =>
        {
            receivedChanges.AddRange(changes);
            eventReceived.TrySetResult(true);
        };

        await _watcher.StartAsync(_testDirectory);

        // Rename the file
        File.Move(oldPath, newPath);

        // Wait for event with timeout
        var received = await Task.WhenAny(eventReceived.Task, Task.Delay(2000)) == eventReceived.Task;

        Assert.Multiple(() =>
        {
            Assert.That(received, Is.True, "Event should be received within timeout");
            Assert.That(receivedChanges.Any(c => c.Kind == FileChangeKind.Renamed && c.FilePath == newPath), Is.True);
        });
    }
}
