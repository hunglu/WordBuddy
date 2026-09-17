using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizById;

public sealed class GetQuizByIdQueryHandler : IQueryHandler<GetQuizByIdQuery, QuizDto>
{
    private readonly IQuizRepository _quizRepository;
    private readonly ILogger<GetQuizByIdQueryHandler> _logger;

    public GetQuizByIdQueryHandler(IQuizRepository quizRepository, ILogger<GetQuizByIdQueryHandler> logger)
    {
        _quizRepository = quizRepository;
        _logger = logger;
    }

    public async Task<Result<QuizDto>> HandleAsync(GetQuizByIdQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetQuizByIdQuery started: QuizId={QuizId}", query.QuizId);

        Result<Domain.Quiz> quizResult = await _quizRepository.GetByIdWithQuestionsAsync(query.QuizId, ct);
        if (quizResult.IsFailure)
        {
            _logger.LogWarning(
                "GetQuizByIdQuery repository failure: {ErrorCode} — {ErrorDescription}",
                quizResult.Error.Code, quizResult.Error.Description);
            return Result.Failure<QuizDto>(quizResult.Error);
        }

        Domain.Quiz quiz = quizResult.Value;

        QuizDto dto = new(
            quiz.Id,
            quiz.LessonId,
            quiz.Title,
            quiz.Description,
            quiz.Level,
            quiz.TargetAgeGroup,
            quiz.Questions.Select(q => new QuizQuestionDto(q.Id, q.Text, q.Type, q.Options)).ToList());

        _logger.LogInformation("GetQuizByIdQuery succeeded: QuizId={QuizId}", quiz.Id);
        return Result.Success(dto);
    }
}
