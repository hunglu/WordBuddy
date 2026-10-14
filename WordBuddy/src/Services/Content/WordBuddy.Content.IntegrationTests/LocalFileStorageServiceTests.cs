using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Interfaces;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-50 <c>IFileStorageService.DeleteAsync</c> on the real local storage (a temp folder
/// that <see cref="ContentApiFactory"/> sets as <c>FileStorage:BasePath</c>).</summary>
[Collection(ContentApiCollection.Name)]
public sealed class LocalFileStorageServiceTests
{
    private static readonly string BasePath = Path.Combine(Path.GetTempPath(), "wordbuddy-content-tests");

    private readonly ContentApiFactory _factory;

    public LocalFileStorageServiceTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(IFileStorageService Storage, string Path)> SaveFileAsync()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IFileStorageService storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        using MemoryStream content = new([1, 2, 3]);
        string path = await storage.SaveNamedAsync(content, $"delete-test-{Guid.NewGuid():N}.mp3", "audio/mpeg");
        return (storage, path);
    }

    [Fact]
    public async Task LocalFileStorageService_DeleteAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        (IFileStorageService storage, string path) = await SaveFileAsync();
        string absolute = Path.Combine(BasePath, path);
        File.Exists(absolute).Should().BeTrue();

        bool deleted = await storage.DeleteAsync(path);

        deleted.Should().BeTrue();
        File.Exists(absolute).Should().BeFalse();
    }

    [Fact]
    public async Task LocalFileStorageService_DeleteAsync_MissingFile_ReturnsFalse()
    {
        (IFileStorageService storage, _) = await SaveFileAsync();

        bool deleted = await storage.DeleteAsync($"audio/missing-{Guid.NewGuid():N}.mp3");

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task LocalFileStorageService_DeleteAsync_PathOutsideBase_IsRefusedAndFileKept()
    {
        (IFileStorageService storage, _) = await SaveFileAsync();
        string outsideName = $"outside-{Guid.NewGuid():N}.mp3";
        string outsideFile = Path.Combine(Path.GetFullPath(Path.Combine(BasePath, "..")), outsideName);
        await File.WriteAllBytesAsync(outsideFile, [1]);
        try
        {
            bool deleted = await storage.DeleteAsync($"../{outsideName}");

            deleted.Should().BeFalse();
            File.Exists(outsideFile).Should().BeTrue();
        }
        finally
        {
            File.Delete(outsideFile);
        }
    }
}
