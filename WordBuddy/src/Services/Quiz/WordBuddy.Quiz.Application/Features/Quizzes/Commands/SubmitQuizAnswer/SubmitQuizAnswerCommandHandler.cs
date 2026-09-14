using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Quiz.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Commands.SubmitQuizAnswer;

public sealed class SubmitQuizAnswerCommandHandler : ICommandHandler<SubmitQuizAnswerCommand, SubmitQuizAnswerResultDto>
{
    private readonly IQuizRepository _quizRepository;
    private readonly IValidator<SubmitQuizAnswerCommand> _validator;
    private readonly ILogger<SubmitQuizAnswerCommandHandler> _logger;

    public SubmitQuizAnswerCommandHandler(
        IQuizRepository quizRepository,
        IValidator<SubmitQuizAnswerCommand> validator,
        ILogger<SubmitQuizAnswerCommandHandler> logger)
    {
        _quizRepository = quizRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<SubmitQuizAnswerResultDto>> HandleAsync(SubmitQuizAnswerCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "SubmitQuizAnswerCommand started: QuizId={QuizId}, QuestionId={QuestionId}",
            command.QuizId, command.QuestionId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SubmitQuizAnswerCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<SubmitQuizAnswerResultDto>(Error.Validation("SubmitQuizAnswer.Validation", validation.ToString()));
        }

        Result<Domain.Quiz> quizResult = await _quizRepository.GetByIdWithQuestionsAsync(command.QuizId, ct);
        if (quizResult.IsFailure)
        {
            _logger.LogWarning("SubmitQuizAnswerCommand quiz not found: QuizId={QuizId}", command.QuizId);
            return Result.Failure<SubmitQuizAnswerResultDto>(quizResult.Error);
        }

        QuizQuestion? question = quizResult.Value.Questions.FirstOrDefault(q => q.Id == command.QuestionId);
        if (question is null)
        {
            _logger.LogWarning(
                "SubmitQuizAnswerCommand question not found: QuizId={QuizId}, QuestionId={QuestionId}",
                command.QuizId, command.QuestionId);
            return Result.Failure<SubmitQuizAnswerResultDto>(
                Error.NotFound("QuizQuestion.NotFound", $"Question {command.QuestionId} was not found in quiz {command.QuizId}."));
        }

        bool isCorrect = question.IsCorrect(command.SelectedOptionIndex);

        _logger.LogInformation(
            "SubmitQuizAnswerCommand succeeded: QuestionId={QuestionId}, IsCorrect={IsCorrect}",
            question.Id, isCorrect);

        return Result.Success(new SubmitQuizAnswerResultDto(isCorrect, question.CorrectOptionIndex, question.Explanation));
    }
}
