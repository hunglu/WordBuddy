using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>Downloads a pronunciation MP3 from a dictionary URL. Failures come back as a failed
/// <see cref="Result"/>. The caller stores the bytes through <see cref="IFileStorageService"/>.</summary>
public interface IAudioDownloader
{
    /// <summary>Downloads the MP3 at <paramref name="url"/>.</summary>
    Task<Result<byte[]>> DownloadAsync(string url, CancellationToken ct = default);
}
