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
}
