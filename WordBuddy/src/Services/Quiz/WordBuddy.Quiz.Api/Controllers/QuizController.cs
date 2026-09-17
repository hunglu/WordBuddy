using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Quiz.Api.Extensions;
using WordBuddy.Quiz.Api.Models;
using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;
using WordBuddy.Quiz.Application.Features.Quizzes.Commands.CreateQuiz;
using WordBuddy.Quiz.Application.Features.Quizzes.Commands.SubmitQuizAnswer;
using WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizById;
using WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizzes;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Api.Controllers;

/// <summary>Quiz definitions, question banks, and answer evaluation.</summary>
[ApiController]
[Route("api/quiz")]
[Authorize]
public sealed class QuizController : ControllerBase
{
    private readonly IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizSummaryDto>> _getQuizzes;
    private readonly IQueryHandler<GetQuizByIdQuery, QuizDto> _getQuizById;
    private readonly ICommandHandler<SubmitQuizAnswerCommand, SubmitQuizAnswerResultDto> _submitAnswer;
    private readonly ICommandHandler<CreateQuizCommand, Guid> _createQuiz;
    private readonly ILogger<QuizController> _logger;

    public QuizController(
        IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizSummaryDto>> getQuizzes,
        IQueryHandler<GetQuizByIdQuery, QuizDto> getQuizById,
        ICommandHandler<SubmitQuizAnswerCommand, SubmitQuizAnswerResultDto> submitAnswer,
        ICommandHandler<CreateQuizCommand, Guid> createQuiz,
        ILogger<QuizController> logger)
    {
        _getQuizzes = getQuizzes;
        _getQuizById = getQuizById;
        _submitAnswer = submitAnswer;
        _createQuiz = createQuiz;
        _logger = logger;
    }

    /// <summary>Lists quizzes, optionally filtered by the lesson they test.</summary>
    [HttpGet]
    public async Task<IActionResult> GetQuizzes([FromQuery] Guid? lessonId, CancellationToken ct)
    {
        Result<IReadOnlyList<QuizSummaryDto>> result = await _getQuizzes.HandleAsync(new GetQuizzesQuery(lessonId), ct);
        return result.IsFailure ? result.ToProblemResult(this) : Ok(result.Value);
    }

    /// <summary>Gets a quiz with its questions (answers not included).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetQuizById(Guid id, CancellationToken ct)
    {
        Result<QuizDto> result = await _getQuizById.HandleAsync(new GetQuizByIdQuery(id), ct);
        if (result.IsFailure)
        {
            _logger.LogWarning("GetQuizById not found: QuizId={QuizId}", id);
            return result.ToProblemResult(this);
        }

        return Ok(result.Value);
    }

    /// <summary>Submits an answer to one question and returns whether it was correct.</summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitAnswer(Guid id, [FromBody] SubmitAnswerRequest request, CancellationToken ct)
    {
        Result<SubmitQuizAnswerResultDto> result = await _submitAnswer.HandleAsync(
            new SubmitQuizAnswerCommand(id, request.QuestionId, request.SelectedOptionIndex), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("SubmitAnswer succeeded: QuizId={QuizId}, IsCorrect={IsCorrect}", id, result.Value.IsCorrect);
        return Ok(result.Value);
    }

    /// <summary>Creates a new quiz with its questions (admin use).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizCommand command, CancellationToken ct)
    {
        Result<Guid> result = await _createQuiz.HandleAsync(command, ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("CreateQuiz succeeded: QuizId={QuizId}", result.Value);
        return CreatedAtAction(nameof(GetQuizById), new { id = result.Value }, result.Value);
    }
}
