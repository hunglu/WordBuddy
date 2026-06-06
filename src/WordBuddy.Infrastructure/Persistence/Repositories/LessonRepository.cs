using Microsoft.EntityFrameworkCore;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Persistence.Repositories;

internal sealed class LessonRepository : ILessonRepository
{
    private readonly WordBuddyDbContext _context;

    public LessonRepository(WordBuddyDbContext context) => _context = context;

    public async Task<Result<Lesson>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        Lesson? lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return lesson is null
            ? Result<Lesson>.Failure(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."))
            : Result<Lesson>.Success(lesson);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetAllAsync(CancellationToken ct = default)
    {
        List<Lesson> lessons = await _context.Lessons.AsNoTracking().ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }

    public async Task<Result> AddAsync(Lesson entity, CancellationToken ct = default)
    {
        await _context.Lessons.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> UpdateAsync(Lesson entity, CancellationToken ct = default)
    {
        _context.Lessons.Update(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        int affected = await _context.Lessons
            .Where(l => l.Id == id)
            .ExecuteDeleteAsync(ct);

        return affected > 0
            ? Result.Success
            : Result.Failure(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetByTypeAsync(LessonType type, CancellationToken ct = default)
    {
        List<Lesson> lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.Type == type)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetByLevelAsync(Level level, CancellationToken ct = default)
    {
        List<Lesson> lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.Level == level)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetPublishedAsync(CancellationToken ct = default)
    {
        List<Lesson> lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.IsPublished)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }
}
