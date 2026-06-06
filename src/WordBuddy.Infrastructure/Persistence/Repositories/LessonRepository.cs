using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Persistence.Repositories;

internal sealed class LessonRepository : ILessonRepository
{
    private readonly WordBuddyDbContext _context;
    private readonly ILogger<LessonRepository> _logger;

    public LessonRepository(WordBuddyDbContext context, ILogger<LessonRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Lesson>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching Lesson by Id={LessonId}", id);

        Lesson? lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (lesson is null)
        {
            _logger.LogWarning("Lesson not found: Id={LessonId}", id);
            return Result<Lesson>.Failure(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
        }

        return Result<Lesson>.Success(lesson);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all Lessons");
        List<Lesson> lessons = await _context.Lessons.AsNoTracking().ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }

    public async Task<Result> AddAsync(Lesson entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding Lesson: Id={LessonId}", entity.Id);
        await _context.Lessons.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> UpdateAsync(Lesson entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating Lesson: Id={LessonId}", entity.Id);
        _context.Lessons.Update(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting Lesson: Id={LessonId}", id);

        int affected = await _context.Lessons
            .Where(l => l.Id == id)
            .ExecuteDeleteAsync(ct);

        if (affected == 0)
        {
            _logger.LogWarning("Delete skipped — Lesson not found: Id={LessonId}", id);
            return Result.Failure(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
        }

        return Result.Success;
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetByTypeAsync(LessonType type, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching Lessons by Type={LessonType}", type);
        List<Lesson> lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.Type == type)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetByLevelAsync(Level level, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching Lessons by Level={Level}", level);
        List<Lesson> lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.Level == level)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetPublishedAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching published Lessons");
        List<Lesson> lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.IsPublished)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Lesson>>.Success(lessons);
    }
}
