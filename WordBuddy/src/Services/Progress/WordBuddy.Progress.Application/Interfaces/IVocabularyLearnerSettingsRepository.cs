using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Stores <see cref="VocabularyLearnerSettings"/>, one row per user.</summary>
public interface IVocabularyLearnerSettingsRepository
{
    /// <summary>Returns the user's settings, or <see cref="ErrorType.NotFound"/> when never saved.</summary>
    Task<Result<VocabularyLearnerSettings>> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Creates or updates the user's settings and saves.</summary>
    Task<Result<VocabularyLearnerSettings>> UpsertAsync(Guid userId, int? newWordsPerDay, CancellationToken ct = default);

    /// <summary>Creates or updates the supporter cap of <paramref name="learnerId"/> and saves.</summary>
    Task<Result<VocabularyLearnerSettings>> UpsertSupporterCapAsync(Guid learnerId, int? cap, Guid supporterId, CancellationToken ct = default);

    /// <summary>Returns the tracked settings (no save), or not found. Changes are saved by the next context save.</summary>
    Task<Result<VocabularyLearnerSettings>> GetTrackedAsync(Guid userId, CancellationToken ct = default);
}
