using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class LessonRepository : ILessonRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<LessonRepository> _logger;

    public LessonRepository(ContentDbContext dbContext, ILogger<LessonRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetPublishedAsync(LessonType? type, Level? level, AgeGroup? targetAgeGroup, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Querying published Lessons: Type={Type}, Level={Level}, TargetAgeGroup={TargetAgeGroup}", type, level, targetAgeGroup);

        IQueryable<Lesson> query = _dbContext.Lessons.AsNoTracking().Where(l => l.IsPublished);
        if (type is not null)
        {
            query = query.Where(l => l.Type == type);
        }
        if (level is not null)
        {
            query = query.Where(l => l.Level == level);
        }

        if (targetAgeGroup is not null)
        {
            query = query.Where(l => l.TargetAgeGroup == targetAgeGroup);
        }

        List<Lesson> lessons = await query.ToListAsync(ct);
        return Result.Success<IReadOnlyList<Lesson>>(lessons);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> ListAllAsync(LessonType? type, Level? level, bool? isPublished, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying all Lessons: Type={Type}, Level={Level}, IsPublished={IsPublished}", type, level, isPublished);

        IQueryable<Lesson> query = _dbContext.Lessons.AsNoTracking().Include(l => l.VocabularyWords);
        if (type is not null)
        {
            query = query.Where(l => l.Type == type);
        }
        if (level is not null)
        {
            query = query.Where(l => l.Level == level);
        }
        if (isPublished is not null)
        {
            query = query.Where(l => l.IsPublished == isPublished);
        }

        List<Lesson> lessons = await query.OrderBy(l => l.Title).ToListAsync(ct);
        return Result.Success<IReadOnlyList<Lesson>>(lessons);
    }

    public async Task<Result<Lesson>> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Loading Lesson for update: LessonId={LessonId}", id);

        Lesson? lesson = await _dbContext.Lessons
            .AsTracking()
            .Include(l => l.VocabularyWords)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return lesson is null
            ? Result.Failure<Lesson>(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."))
            : Result.Success(lesson);
    }

    public void Remove(Lesson lesson) => _dbContext.Lessons.Remove(lesson);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Two admins attached the same word to the same lesson at once (PK of LessonSenses).
            _logger.LogWarning("Lesson save hit a unique-key violation on LessonSenses");
            return Result.Failure(Error.Conflict(
                "Lesson.SenseAlreadyAttached", "This word is already attached to the lesson."));
        }
    }

    public async Task<Result<Lesson>> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying Lesson with details: LessonId={LessonId}", id);

        Lesson? lesson = await _dbContext.Lessons
            .AsNoTracking()
            .Include(l => l.VocabularyWords.OrderBy(v => v.SortOrder)).ThenInclude(v => v.Sense!).ThenInclude(w => w.Audio)
            .Include(l => l.VocabularyWords).ThenInclude(v => v.Sense!).ThenInclude(w => w.Lexeme!).ThenInclude(x => x.UkAudio)
            .Include(l => l.VocabularyWords).ThenInclude(v => v.Sense!).ThenInclude(w => w.Lexeme!).ThenInclude(x => x.UsAudio)
            .Include(l => l.VocabularyWords).ThenInclude(v => v.Sense!).ThenInclude(w => w.Translations)
            .AsSplitQuery()
            .Include(l => l.GrammarRules)
            .Include(l => l.DailyPhrases).ThenInclude(d => d.Audio)
            .Include(l => l.DailyPhrases).ThenInclude(d => d.Video)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (lesson is null)
        {
            _logger.LogWarning("Lesson not found: LessonId={LessonId}", id);
            return Result.Failure<Lesson>(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
        }

        return Result.Success(lesson);
    }

    public async Task<Result> AddAsync(Lesson lesson, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding Lesson with Id={LessonId}", lesson.Id);

        await _dbContext.Lessons.AddAsync(lesson, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
