namespace WordBuddy.Application.Interfaces;

/// <summary>Abstraction for storing and retrieving media asset binary files.</summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves <paramref name="fileStream"/> to persistent storage and returns the relative file path.
    /// </summary>
    /// <param name="fileStream">Readable stream of the file content.</param>
    /// <param name="fileName">Original file name — used to derive the extension.</param>
    /// <param name="contentType">MIME type — used to organise files into type sub-directories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The relative path at which the file was stored.</returns>
    Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Opens a readable <see cref="Stream"/> for the file at <paramref name="filePath"/>.
    /// Throws <see cref="FileNotFoundException"/> if the file does not exist on disk.
    /// </summary>
    /// <param name="filePath">Relative path returned by <see cref="SaveAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Stream> GetAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Deletes the file at <paramref name="filePath"/>. A no-op if the file does not exist.
    /// </summary>
    /// <param name="filePath">Relative path returned by <see cref="SaveAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string filePath, CancellationToken ct = default);
}
