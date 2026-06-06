using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Interfaces;

/// <summary>Read repository for fetching <see cref="DailyPhrase"/> entities belonging to a lesson.</summary>
public interface IDailyPhraseRepository
{
    /// <summary>Retrieves all daily phrases belonging to the specified lesson.</summary>
    Task<Result<IReadOnlyList<DailyPhrase>>> GetByLessonIdAsync(Guid lessonId, CancellationToken ct = default);
}
