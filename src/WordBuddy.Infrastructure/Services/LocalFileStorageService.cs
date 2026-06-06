using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordBuddy.Application.Interfaces;
using WordBuddy.Infrastructure.Settings;

namespace WordBuddy.Infrastructure.Services;

/// <summary>
/// Stores media files on the local file system under a configurable base path.
/// Configured via the <c>FileStorage</c> section in application settings.
/// Files are organised as <c>BasePath/{type}/{year}/{month}/{guid+ext}</c>.
/// </summary>
internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    /// <summary>Initializes a new <see cref="LocalFileStorageService"/>.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>FileStorage:BasePath</c> is absent or empty in configuration.
    /// </exception>
    public LocalFileStorageService(IOptions<FileStorageSettings> options, ILogger<LocalFileStorageService> logger)
    {
        string basePath = options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(basePath))
            throw new InvalidOperationException(
                "'FileStorage:BasePath' configuration is required but was not set.");
        _basePath = basePath;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        string typeFolder = GetTypeFolder(contentType);
        string year = DateTime.UtcNow.Year.ToString();
        string month = DateTime.UtcNow.Month.ToString("D2");
        string extension = Path.GetExtension(fileName);
        string uniqueFileName = $"{Guid.NewGuid()}{extension}";
        string relativePath = $"{typeFolder}/{year}/{month}/{uniqueFileName}";

        string absoluteDir = Path.Combine(_basePath, typeFolder, year, month);
        Directory.CreateDirectory(absoluteDir);

        string absoluteFile = Path.Combine(absoluteDir, uniqueFileName);

        _logger.LogInformation(
            "Saving media file: OriginalFileName={OriginalFileName}, ContentType={ContentType}, Destination={RelativePath}",
            fileName, contentType, relativePath);

        await using FileStream destination = new(absoluteFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(destination, ct);

        _logger.LogInformation("Media file saved: RelativePath={RelativePath}", relativePath);
        return relativePath;
    }

    /// <inheritdoc/>
    public Task<Stream> GetAsync(string filePath, CancellationToken ct = default)
    {
        string absolutePath = Path.Combine(_basePath, filePath.Replace('/', Path.DirectorySeparatorChar));

        _logger.LogDebug("Opening media file: RelativePath={RelativePath}", filePath);

        if (!File.Exists(absolutePath))
        {
            _logger.LogWarning("Media file not found on disk: RelativePath={RelativePath}", filePath);
            throw new FileNotFoundException($"Media file '{filePath}' was not found.", absolutePath);
        }

        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        string absolutePath = Path.Combine(_basePath, filePath.Replace('/', Path.DirectorySeparatorChar));

        _logger.LogInformation("Deleting media file: RelativePath={RelativePath}", filePath);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            _logger.LogInformation("Media file deleted: RelativePath={RelativePath}", filePath);
        }
        else
        {
            _logger.LogWarning("Delete skipped — media file not found on disk: RelativePath={RelativePath}", filePath);
        }

        return Task.CompletedTask;
    }

    private static string GetTypeFolder(string contentType) =>
        contentType.Split('/')[0] switch
        {
            "image" => "images",
            "audio" => "audio",
            "video" => "video",
            _       => "other",
        };
}
