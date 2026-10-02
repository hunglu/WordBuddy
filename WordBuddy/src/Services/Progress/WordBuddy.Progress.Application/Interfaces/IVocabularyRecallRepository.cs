using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

public interface IVocabularyRecallRepository
{
    /// <summary>Inserts or updates the <see cref="VocabularyRecallStat"/> for each result — one
    /// row per (<paramref name="userId"/>, word), applying <see cref="VocabularyRecallStat.ApplyCheckResult"/>
    /// to an existing row or creating a new one.</summary>
    Task<Result> UpsertStatsAsync(Guid userId, IReadOnlyList<VocabularyRecallResult> results, CancellationToken ct = default);

    Task<Result> AddSessionAsync(VocabularyRecallSession session, CancellationToken ct = default);

    Task<Result<IReadOnlyList<VocabularyRecallStat>>> GetStatsByUserAsync(Guid userId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<VocabularyRecallSession>>> GetRecentSessionsByUserAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>Returns change-tracked stats (all users) whose <see cref="VocabularyRecallStat.VocabularyWordId"/>
    /// is in <paramref name="vocabularyWordIds"/>.</summary>
    Task<Result<IReadOnlyList<VocabularyRecallStat>>> GetTrackedStatsByWordIdsAsync(IReadOnlyCollection<Guid> vocabularyWordIds, CancellationToken ct = default);

    /// <summary>Deletes <paramref name="statsToRemove"/> and saves every change made to stats returned
    /// by <see cref="GetTrackedStatsByWordIdsAsync"/>, in one save.</summary>
    Task<Result> SaveRemappedStatsAsync(IReadOnlyList<VocabularyRecallStat> statsToRemove, CancellationToken ct = default);
}

/// <summary>One word's outcome within a recall-check submission.</summary>
public sealed record VocabularyRecallResult(Guid VocabularyWordId, string Word, bool Known);
