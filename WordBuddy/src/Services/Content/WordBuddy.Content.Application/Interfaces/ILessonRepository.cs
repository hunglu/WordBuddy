using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

public interface ILessonRepository
{
    /// <summary>Returns published lessons, optionally filtered by <paramref name="type"/>, <paramref name="level"/>
    /// and/or <paramref name="targetAgeGroup"/> (<see langword="null"/> = no age filter).</summary>
    Task<Result<IReadOnlyList<Lesson>>> GetPublishedAsync(LessonType? type, Level? level, AgeGroup? targetAgeGroup, CancellationToken ct = default);

    /// <summary>Returns drafts and published lessons (vocabulary links loaded, no tracking), optionally
    /// filtered by type, level and publish state. Ordered by title.</summary>
    Task<Result<PagedResult<Lesson>>> ListAllAsync(LessonType? type, Level? level, bool? isPublished, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Returns a change-tracked lesson with its <see cref="Lesson.VocabularyWords"/> loaded, or
    /// <see cref="Error.NotFound"/>. Persist changes with <see cref="SaveChangesAsync"/>.</summary>
    Task<Result<Lesson>> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Marks a lesson returned by <see cref="GetByIdForUpdateAsync"/> for deletion. Its links
    /// are removed with it; the senses stay.</summary>
    void Remove(Lesson lesson);

    /// <summary>Saves the changes made to tracked lessons.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Returns a single lesson with its Vocabulary/Grammar/DailyPhrase children loaded, or <see cref="Error.NotFound"/>.</summary>
    Task<Result<Lesson>> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<Result> AddAsync(Lesson lesson, CancellationToken ct = default);
}
