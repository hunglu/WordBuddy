using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Quiz.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Infrastructure.Repositories;

internal sealed class QuizRepository : IQuizRepository
{
    private readonly QuizDbContext _dbContext;
    private readonly ILogger<QuizRepository> _logger;

    public QuizRepository(QuizDbContext dbContext, ILogger<QuizRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<Domain.Quiz>>> GetAllAsync(Guid? lessonId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying Quizzes: LessonId={LessonId}", lessonId);

        IQueryable<Domain.Quiz> query = _dbContext.Quizzes.AsNoTracking();
        if (lessonId is not null)
        {
            query = query.Where(q => q.LessonId == lessonId);
        }

        List<Domain.Quiz> quizzes = await query.ToListAsync(ct);
        return Result.Success<IReadOnlyList<Domain.Quiz>>(quizzes);
    }

    public async Task<Result<Domain.Quiz>> GetByIdWithQuestionsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying Quiz with questions: QuizId={QuizId}", id);

        Domain.Quiz? quiz = await _dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        if (quiz is null)
        {
            _logger.LogWarning("Quiz not found: QuizId={QuizId}", id);
            return Result.Failure<Domain.Quiz>(Error.NotFound("Quiz.NotFound", $"Quiz {id} was not found."));
        }

        return Result.Success(quiz);
    }

    public async Task<Result> AddAsync(Domain.Quiz quiz, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding Quiz with Id={QuizId}", quiz.Id);

        await _dbContext.Quizzes.AddAsync(quiz, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
