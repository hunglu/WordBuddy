using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin;

/// <summary>Deletes the file of a media asset row that was just deleted, unless another row still
/// points to the same file. Best effort: a failure is a warning, never an error for the caller.</summary>
internal static class LexemeAudioCleanup
{
    public static async Task DeleteFileIfUnusedAsync(
        string deletedUrl,
        Guid assetId,
        ILexemeAdminRepository repository,
        IFileStorageService fileStorage,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            Result<bool> used = await repository.IsUrlUsedAsync(deletedUrl, ct);
            if (used.IsFailure || used.Value)
            {
                logger.LogInformation("Lexeme audio file kept: AssetId={AssetId}", assetId);
                return;
            }

            bool deleted = await fileStorage.DeleteAsync(deletedUrl, ct);
            if (!deleted)
            {
                logger.LogWarning("Lexeme audio file was not deleted (missing or refused): AssetId={AssetId}", assetId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Lexeme audio file delete failed: AssetId={AssetId}", assetId);
        }
    }
}
