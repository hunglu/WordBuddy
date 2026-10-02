using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Read/acknowledge access to the word-id remaps recorded by the
/// <c>UnifyVocabularyWords</c> migration, pulled by Progress over the internal endpoint.</summary>
public interface IVocabularyWordIdRemapRepository
{
    /// <summary>Returns at most <paramref name="limit"/> remaps not yet acknowledged by Progress,
    /// ordered by old id.</summary>
    Task<Result<IReadOnlyList<VocabularyWordIdRemapDto>>> GetPendingAsync(int limit, CancellationToken ct = default);

    /// <summary>Marks the listed remaps as acknowledged now, in one set-based update. Ids that are
    /// unknown or already acknowledged are ignored, so repeated calls are harmless. Returns the number
    /// of rows stamped.</summary>
    Task<Result<int>> AcknowledgeAsync(IReadOnlyCollection<Guid> oldIds, CancellationToken ct = default);
}
