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

    public Task<Stream> GetAsync(string relativePath, CancellationToken ct = default)
    {
        string absolutePath = Path.Combine(_settings.BasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        _logger.LogDebug("Opening media file: RelativePath={RelativePath}", relativePath);
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }
}
