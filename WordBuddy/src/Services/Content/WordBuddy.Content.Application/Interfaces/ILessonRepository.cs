using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

public interface ILessonRepository
{
    /// <summary>Returns published lessons, optionally filtered by <paramref name="type"/> and/or <paramref name="level"/>.</summary>
    Task<Result<IReadOnlyList<Lesson>>> GetPublishedAsync(LessonType? type, Level? level, CancellationToken ct = default);

    /// <summary>Returns a single lesson with its Vocabulary/Grammar/DailyPhrase children loaded, or <see cref="Error.NotFound"/>.</summary>
    Task<Result<Lesson>> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<Result> AddAsync(Lesson lesson, CancellationToken ct = default);
}
