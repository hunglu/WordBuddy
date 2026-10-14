namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Persists uploaded media binaries. The database only ever stores the returned relative path.</summary>
public interface IFileStorageService
{
    /// <summary>Saves <paramref name="content"/> and returns its relative storage path.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Saves <paramref name="content"/> under the exact <paramref name="fileName"/> (for example
    /// <c>lexeme-{id}-en-GB.mp3</c>), overwriting an old file, and returns its relative storage path.</summary>
    Task<string> SaveNamedAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Opens the file at <paramref name="relativePath"/> for reading.</summary>
    Task<Stream> GetAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Deletes the file at <paramref name="relativePath"/>. Returns <see langword="true"/> when
    /// a file was deleted, and <see langword="false"/> when the file did not exist or the path was
    /// refused (outside the storage folder).</summary>
    Task<bool> DeleteAsync(string relativePath, CancellationToken ct = default);
}
