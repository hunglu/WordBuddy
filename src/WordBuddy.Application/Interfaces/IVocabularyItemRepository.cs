using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Interfaces;

/// <summary>Read repository for fetching <see cref="VocabularyItem"/> entities belonging to a lesson.</summary>
public interface IVocabularyItemRepository
{
    /// <summary>Retrieves all vocabulary items belonging to the specified lesson.</summary>
    Task<Result<IReadOnlyList<VocabularyItem>>> GetByLessonIdAsync(Guid lessonId, CancellationToken ct = default);
}
