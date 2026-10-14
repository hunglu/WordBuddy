using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Extensions;

/// <summary>Shared steps for the add-to-my-list handlers.</summary>
public static class VocabularyWordRepositoryExtensions
{
    /// <summary>Handles "the caller is already linked": a non-blank note overwrites the stored note
    /// and is saved. A blank note changes nothing. Failures are logged by error code only —
    /// the note text is never logged.</summary>
    public static async Task<Result> ApplyNoteToExistingLinkAsync(
        this IVocabularyWordRepository repository,
        LearnerWord link,
        string? personalContext,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(personalContext))
        {
            return Result.Success();
        }

        Result setResult = link.SetPersonalContext(personalContext);
        if (setResult.IsFailure)
        {
            logger.LogWarning("Personal note rejected on existing link: {ErrorCode}", setResult.Error.Code);
            return setResult;
        }

        Result saveResult = await repository.SaveLinkAsync(link, ct);
        if (saveResult.IsFailure)
        {
            logger.LogWarning("Personal note save failed on existing link: {ErrorCode}", saveResult.Error.Code);
        }

        return saveResult;
    }
}
