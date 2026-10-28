using EnglishLiveTutor.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class FileLessonMediaStoragePathTraversalTests : IDisposable
{
    private readonly string _parent = Path.Combine(Path.GetTempPath(), "ep-media-" + Guid.NewGuid().ToString("N"));
    private readonly string _root;
    private readonly FileLessonMediaStorage _storage;

    public FileLessonMediaStoragePathTraversalTests()
    {
        _root = Path.Combine(_parent, "lesson-media");
        Directory.CreateDirectory(_root);
        _storage = new FileLessonMediaStorage(
            Options.Create(new LessonMediaStorageOptions { RootPath = _root }),
            NullLogger<FileLessonMediaStorage>.Instance);
    }

    [Theory]
    [InlineData("../lesson-media-evil/secret.webm")]
    [InlineData("media/../../lesson-media-evil/secret.webm")]
    [InlineData("....//lesson-media-evil/secret.webm")]
    public async Task Keys_ThatEscapeTheRoot_AreNotResolved(string storageKey)
    {
        string sibling = Path.Combine(_parent, "lesson-media-evil");
        Directory.CreateDirectory(sibling);
        await File.WriteAllTextAsync(Path.Combine(sibling, "secret.webm"), "x");

        Assert.False(await _storage.ExistsAsync(storageKey));
        Assert.Null(await _storage.OpenReadAsync(storageKey));
    }

    [Fact]
    public async Task RootedKey_IsNotResolved()
    {
        string outside = Path.Combine(_parent, "outside.webm");
        await File.WriteAllTextAsync(outside, "x");

        Assert.False(await _storage.ExistsAsync(outside));
    }

    [Fact]
    public async Task KeyInsideRoot_IsResolved()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "ok.webm"), "x");

        Assert.True(await _storage.ExistsAsync("media/ok.webm"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_parent, recursive: true); } catch { }
    }
}
