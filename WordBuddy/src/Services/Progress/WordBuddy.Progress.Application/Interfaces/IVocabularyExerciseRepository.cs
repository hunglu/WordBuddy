using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Store for <see cref="VocabularyExercise"/>. No delete method; <see cref="VocabularyExercise.Answer"/> is the only change.</summary>
public interface IVocabularyExerciseRepository
{
    /// <summary>Stages a new exercise. Committed by <see cref="ILearnerWordStateRepository.SaveChangesAsync"/>.</summary>
    Task<Result> AddAsync(VocabularyExercise exercise, CancellationToken ct = default);

    /// <summary>Returns the change-tracked exercise, or <see cref="ErrorType.NotFound"/>.</summary>
    Task<Result<VocabularyExercise>> GetTrackedAsync(Guid exerciseId, CancellationToken ct = default);
}
