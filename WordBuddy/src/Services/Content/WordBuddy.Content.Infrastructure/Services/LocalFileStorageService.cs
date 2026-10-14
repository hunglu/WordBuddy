using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Infrastructure.Settings;

namespace WordBuddy.Content.Infrastructure.Services;

internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IOptions<FileStorageSettings> settings, ILogger<LocalFileStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;
        string extension = Path.GetExtension(fileName);
        string storedFileName = $"{Guid.NewGuid()}{extension}";
        string relativePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), storedFileName);
        string absolutePath = Path.Combine(_settings.BasePath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using (FileStream fileStream = File.Create(absolutePath))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        _logger.LogInformation("Saved media file: RelativePath={RelativePath}", relativePath);
        return relativePath.Replace('\\', '/');
    }

    public async Task<string> SaveNamedAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // GetFileName strips any directory part, so a name can never escape the audio folder.
        string safeName = Path.GetFileName(fileName);
        string relativePath = Path.Combine("audio", safeName);
        string absolutePath = Path.Combine(_settings.BasePath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using (FileStream fileStream = File.Create(absolutePath))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        _logger.LogInformation("Saved named media file: RelativePath={RelativePath}", relativePath);
        return relativePath.Replace('\\', '/');
    }

    // Windows paths ignore case; Linux paths do not, so the guard must not either.
    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public Task<bool> DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        string basePath = Path.GetFullPath(_settings.BasePath);
        string absolutePath = Path.GetFullPath(Path.Combine(basePath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        string baseWithSeparator = basePath.EndsWith(Path.DirectorySeparatorChar) ? basePath : basePath + Path.DirectorySeparatorChar;

        // Path-traversal guard: only files strictly inside the storage folder may be deleted.
        if (!absolutePath.StartsWith(baseWithSeparator, PathComparison))
        {
            _logger.LogWarning("Refused to delete a media file outside the storage folder");
            return Task.FromResult(false);
        }

        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(absolutePath);
        _logger.LogInformation("Deleted media file: RelativePath={RelativePath}", relativePath);
        return Task.FromResult(true);
    }

    public Task<Stream> GetAsync(string relativePath, CancellationToken ct = default)
    {
        string absolutePath = Path.Combine(_settings.BasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        _logger.LogDebug("Opening media file: RelativePath={RelativePath}", relativePath);
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }
}
