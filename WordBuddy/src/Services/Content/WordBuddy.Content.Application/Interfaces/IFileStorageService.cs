namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Persists uploaded media binaries. The database only ever stores the returned relative path.</summary>
public interface IFileStorageService
{
    /// <summary>Saves <paramref name="content"/> and returns its relative storage path.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Opens the file at <paramref name="relativePath"/> for reading.</summary>
    Task<Stream> GetAsync(string relativePath, CancellationToken ct = default);
}
