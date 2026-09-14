using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Quiz.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Commands.CreateQuiz;

public sealed class CreateQuizCommandHandler : ICommandHandler<CreateQuizCommand, Guid>
{
    private readonly IQuizRepository _quizRepository;
    private readonly IValidator<CreateQuizCommand> _validator;
    private readonly ILogger<CreateQuizCommandHandler> _logger;

    public CreateQuizCommandHandler(
        IQuizRepository quizRepository,
        IValidator<CreateQuizCommand> validator,
        ILogger<CreateQuizCommandHandler> logger)
    {
        _quizRepository = quizRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateQuizCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateQuizCommand started: Title={Title}, LessonId={LessonId}, QuestionCount={QuestionCount}",
            command.Title, command.LessonId, command.Questions.Count);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateQuizCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("CreateQuiz.Validation", validation.ToString()));
        }

        Guid id = Guid.NewGuid();
        Domain.Quiz quiz = new(id, command.LessonId, command.Title, command.Description, command.Level, command.TargetAgeGroup);

        foreach (CreateQuizQuestionInput input in command.Questions)
        {
            quiz.AddQuestion(new QuizQuestion(
                Guid.NewGuid(), id, input.Text, input.Type, input.Options, input.CorrectOptionIndex, input.Explanation));
        }

        Result addResult = await _quizRepository.AddAsync(quiz, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "CreateQuizCommand failed to persist quiz: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<Guid>(addResult.Error);
        }

        _logger.LogInformation("CreateQuizCommand succeeded: QuizId={QuizId}", id);
        return Result.Success(id);
    }
}
