using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Insert-only store for <see cref="VocabularySessionIssue"/>. No update or delete by design.</summary>
public interface IVocabularySessionIssueRepository
{
    /// <summary>Stages a new row. Committed by <see cref="ILearnerWordStateRepository.SaveChangesAsync"/>.</summary>
    Task<Result> AddAsync(VocabularySessionIssue issue, CancellationToken ct = default);
}
