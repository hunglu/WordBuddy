using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.Interfaces;

/// <summary>Repository contract for <see cref="Lesson"/> entities.</summary>
public interface ILessonRepository : IRepository<Lesson>
{
    /// <summary>Retrieves all lessons of the specified content type.</summary>
    Task<Result<IReadOnlyList<Lesson>>> GetByTypeAsync(LessonType type, CancellationToken ct = default);

    /// <summary>Retrieves all lessons targeting the specified proficiency level.</summary>
    Task<Result<IReadOnlyList<Lesson>>> GetByLevelAsync(Level level, CancellationToken ct = default);

    /// <summary>Retrieves all published lessons, ordered by <see cref="Lesson.OrderIndex"/>.</summary>
    Task<Result<IReadOnlyList<Lesson>>> GetPublishedAsync(CancellationToken ct = default);
}
