using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Api.Models;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerWordStates;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySession;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySettings;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Api.Controllers;

/// <summary>Vocabulary SRS: daily session, answers, word states and settings. Caller's own data
/// only. Same endpoints for child and adult; child grading is more lenient (D-7).</summary>
[ApiController]
[Route("api/progress/vocabulary")]
[Authorize]
public sealed class VocabularySrsController : ControllerBase
{
    private readonly IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto> _getSession;
    private readonly ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto> _recordReview;
    private readonly IQueryHandler<GetLearnerWordStatesQuery, IReadOnlyList<LearnerWordStateDto>> _getWords;
    private readonly IQueryHandler<GetVocabularySettingsQuery, VocabularySettingsDto> _getSettings;
    private readonly ICommandHandler<UpdateVocabularySettingsCommand, VocabularySettingsDto> _updateSettings;
    private readonly ILogger<VocabularySrsController> _logger;

    public VocabularySrsController(
        IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto> getSession,
        ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto> recordReview,
        IQueryHandler<GetLearnerWordStatesQuery, IReadOnlyList<LearnerWordStateDto>> getWords,
        IQueryHandler<GetVocabularySettingsQuery, VocabularySettingsDto> getSettings,
        ICommandHandler<UpdateVocabularySettingsCommand, VocabularySettingsDto> updateSettings,
        ILogger<VocabularySrsController> logger)
    {
        _getSession = getSession;
        _recordReview = recordReview;
        _getWords = getWords;
        _getSettings = getSettings;
        _updateSettings = updateSettings;
        _logger = logger;
    }

    /// <summary>Gets today's session: due words first, then new words up to the cap. The
    /// <c>X-Client-CurrentDateTime</c> header sets the client's day (offset only); missing → UTC.</summary>
    [HttpGet("session")]
    public async Task<IActionResult> GetSession(
        [FromHeader(Name = ClientDateTime.HeaderName)] string? clientCurrentDateTime,
        CancellationToken ct)
    {
        Result<VocabularySessionDto> result = await _getSession.HandleAsync(
            new GetVocabularySessionQuery(User.GetUserId(), clientCurrentDateTime), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Records one answer. The server derives rating, due flag and attempt number.</summary>
    [HttpPost("reviews")]
    [EnableRateLimiting(RateLimitingConfiguration.VocabularyReviewPolicy)]
    public async Task<IActionResult> RecordReview([FromBody] RecordVocabularyReviewRequest request, CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result<VocabularyReviewResultDto> result = await _recordReview.HandleAsync(
            new RecordVocabularyReviewCommand(
                userId,
                User.GetAgeGroup(),
                request.SessionId,
                request.SenseId,
                request.ExerciseType,
                request.Skill,
                request.IsCorrect,
                request.ResponseMs,
                request.HintUsed),
            ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("RecordReview succeeded: UserId={UserId}, SenseId={SenseId}", userId, request.SenseId);
        return Ok(result.Value);
    }

    /// <summary>Gets the caller's active word states.</summary>
    [HttpGet("words")]
    public async Task<IActionResult> GetWords(CancellationToken ct)
    {
        Result<IReadOnlyList<LearnerWordStateDto>> result = await _getWords.HandleAsync(new GetLearnerWordStatesQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Gets the caller's vocabulary settings.</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        Result<VocabularySettingsDto> result = await _getSettings.HandleAsync(new GetVocabularySettingsQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Sets the caller's daily new-word cap (0–50, or null for the backlog rule). Any user (D-4).</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateVocabularySettingsRequest request, CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result<VocabularySettingsDto> result = await _updateSettings.HandleAsync(
            new UpdateVocabularySettingsCommand(userId, request.NewWordsPerDay), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("UpdateSettings succeeded: UserId={UserId}", userId);
        return Ok(result.Value);
    }
}
