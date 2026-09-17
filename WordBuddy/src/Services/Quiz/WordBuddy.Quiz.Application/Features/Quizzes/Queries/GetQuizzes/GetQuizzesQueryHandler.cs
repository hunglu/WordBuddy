using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizzes;

public sealed class GetQuizzesQueryHandler : IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizSummaryDto>>
{
    private readonly IQuizRepository _quizRepository;
    private readonly ILogger<GetQuizzesQueryHandler> _logger;

    public GetQuizzesQueryHandler(IQuizRepository quizRepository, ILogger<GetQuizzesQueryHandler> logger)
    {
        _quizRepository = quizRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<QuizSummaryDto>>> HandleAsync(GetQuizzesQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetQuizzesQuery started: LessonId={LessonId}", query.LessonId);

        Result<IReadOnlyList<Domain.Quiz>> quizzesResult = await _quizRepository.GetAllAsync(query.LessonId, ct);
        if (quizzesResult.IsFailure)
        {
            _logger.LogWarning(
                "GetQuizzesQuery repository failure: {ErrorCode} — {ErrorDescription}",
                quizzesResult.Error.Code, quizzesResult.Error.Description);
            return Result.Failure<IReadOnlyList<QuizSummaryDto>>(quizzesResult.Error);
        }

        IReadOnlyList<QuizSummaryDto> dtos = quizzesResult.Value
            .Select(q => new QuizSummaryDto(q.Id, q.LessonId, q.Title, q.Description, q.Level, q.TargetAgeGroup))
            .ToList();

        _logger.LogInformation("GetQuizzesQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }
}
