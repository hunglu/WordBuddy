using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Progress.Api.Authorization;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Api.Models;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.CreateVocabularyExercise;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateLearnerVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerWordStates;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySession;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySettings;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Api.Controllers;

/// <summary>Vocabulary SRS: daily session, answers, word states and settings. Caller's own data
/// only, except the supporter settings endpoints (<c>learners/{learnerId}/settings</c>, active supporter).
/// Same endpoints for child and adult; child grading is more lenient (D-7). A child without an active
/// supporter gets 403 <c>Learner.SupporterRequired</c> on session and reviews (WB-24).</summary>
[ApiController]
[Route("api/progress/vocabulary")]
[Authorize]
public sealed class VocabularySrsController : ControllerBase
{
    private readonly IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto> _getSession;
    private readonly ICommandHandler<CreateVocabularyExerciseCommand, VocabularyExerciseDto> _createExercise;
    private readonly ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto> _recordReview;
    private readonly IQueryHandler<GetLearnerWordStatesQuery, IReadOnlyList<LearnerWordStateDto>> _getWords;
    private readonly IQueryHandler<GetVocabularySettingsQuery, VocabularySettingsDto> _getSettings;
    private readonly ICommandHandler<UpdateVocabularySettingsCommand, VocabularySettingsDto> _updateSettings;
    private readonly IQueryHandler<GetLearnerVocabularySettingsQuery, LearnerVocabularySettingsDto> _getLearnerSettings;
    private readonly ICommandHandler<UpdateLearnerVocabularySettingsCommand, LearnerVocabularySettingsDto> _updateLearnerSettings;
    private readonly ILogger<VocabularySrsController> _logger;

    public VocabularySrsController(
        IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto> getSession,
        ICommandHandler<CreateVocabularyExerciseCommand, VocabularyExerciseDto> createExercise,
        ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto> recordReview,
        IQueryHandler<GetLearnerWordStatesQuery, IReadOnlyList<LearnerWordStateDto>> getWords,
        IQueryHandler<GetVocabularySettingsQuery, VocabularySettingsDto> getSettings,
        ICommandHandler<UpdateVocabularySettingsCommand, VocabularySettingsDto> updateSettings,
        IQueryHandler<GetLearnerVocabularySettingsQuery, LearnerVocabularySettingsDto> getLearnerSettings,
        ICommandHandler<UpdateLearnerVocabularySettingsCommand, LearnerVocabularySettingsDto> updateLearnerSettings,
        ILogger<VocabularySrsController> logger)
    {
        _getSession = getSession;
        _createExercise = createExercise;
        _recordReview = recordReview;
        _getWords = getWords;
        _getSettings = getSettings;
        _updateSettings = updateSettings;
        _getLearnerSettings = getLearnerSettings;
        _updateLearnerSettings = updateLearnerSettings;
        _logger = logger;
    }

    /// <summary>Gets today's session: due words first, then new words up to the cap. The
    /// <c>X-Client-CurrentDateTime</c> header sets the client's day (offset only); missing → UTC.</summary>
    [HttpGet("session")]
    [Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
    public async Task<IActionResult> GetSession(
        [FromHeader(Name = ClientDateTime.HeaderName)] string? clientCurrentDateTime,
        CancellationToken ct)
    {
        Result<VocabularySessionDto> result = await _getSession.HandleAsync(
            new GetVocabularySessionQuery(User.GetUserId(), clientCurrentDateTime), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Builds the next exercise for one session word. The reply has the prompt and options, never the answer.</summary>
    [HttpPost("exercises")]
    [Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
    [EnableRateLimiting(RateLimitingConfiguration.VocabularyReviewPolicy)]
    public async Task<IActionResult> CreateExercise([FromBody] CreateVocabularyExerciseRequest request, CancellationToken ct)
    {
        Result<VocabularyExerciseDto> result = await _createExercise.HandleAsync(
            new CreateVocabularyExerciseCommand(User.GetUserId(), request.SessionId, request.SenseId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Records one answer to an issued exercise. The server checks the answer and derives correctness,
    /// response time, rating, due flag and attempt number.</summary>
    [HttpPost("reviews")]
    [Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
    [EnableRateLimiting(RateLimitingConfiguration.VocabularyReviewPolicy)]
    public async Task<IActionResult> RecordReview([FromBody] RecordVocabularyReviewRequest request, CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result<VocabularyReviewResultDto> result = await _recordReview.HandleAsync(
            new RecordVocabularyReviewCommand(
                userId,
                User.GetAgeGroup(),
                request.ExerciseId,
                new ReviewAnswer(request.Answer.OptionKey, request.Answer.Text),
                request.ClientResponseMs,
                request.HintUsed),
            ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("RecordReview succeeded: UserId={UserId}, ExerciseId={ExerciseId}", userId, request.ExerciseId);
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

    /// <summary>Gets a learner settings. Caller must be an active supporter of the learner.</summary>
    [HttpGet("learners/{learnerId:guid}/settings")]
    [Authorize(Policy = SupportLinkPolicies.CanSupportLearner)]
    public async Task<IActionResult> GetLearnerSettings(Guid learnerId, CancellationToken ct)
    {
        Result<LearnerVocabularySettingsDto> result = await _getLearnerSettings.HandleAsync(
            new GetLearnerVocabularySettingsQuery(User.GetUserId(), learnerId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Sets or clears the supporter new-word cap of a learner. Any active supporter (Q7).</summary>
    [HttpPut("learners/{learnerId:guid}/settings")]
    [Authorize(Policy = SupportLinkPolicies.CanSupportLearner)]
    public async Task<IActionResult> UpdateLearnerSettings(
        Guid learnerId,
        [FromBody] UpdateLearnerVocabularySettingsRequest request,
        CancellationToken ct)
    {
        Guid supporterId = User.GetUserId();

        Result<LearnerVocabularySettingsDto> result = await _updateLearnerSettings.HandleAsync(
            new UpdateLearnerVocabularySettingsCommand(supporterId, learnerId, request.SupporterNewWordCap), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("UpdateLearnerSettings succeeded: SupporterId={SupporterId}, LearnerId={LearnerId}", supporterId, learnerId);
        return Ok(result.Value);
    }
}
