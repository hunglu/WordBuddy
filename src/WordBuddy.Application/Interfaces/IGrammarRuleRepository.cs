using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Interfaces;

/// <summary>Read repository for fetching <see cref="GrammarRule"/> entities belonging to a lesson.</summary>
public interface IGrammarRuleRepository
{
    /// <summary>Retrieves all grammar rules belonging to the specified lesson, ordered by <see cref="GrammarRule.OrderIndex"/>.</summary>
    Task<Result<IReadOnlyList<GrammarRule>>> GetByLessonIdAsync(Guid lessonId, CancellationToken ct = default);
}
