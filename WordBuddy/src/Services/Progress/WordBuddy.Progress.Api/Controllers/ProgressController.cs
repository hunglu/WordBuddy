using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Api.Models;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Api.Controllers;

/// <summary>Learner progress tracking — completion and scores across lessons.</summary>
[ApiController]
[Route("api/progress")]
[Authorize]
public sealed class ProgressController : ControllerBase
{
    private readonly ICommandHandler<RecordProgressCommand> _recordProgress;
    private readonly IQueryHandler<GetUserProgressQuery, IReadOnlyList<LearnerProgressDto>> _getUserProgress;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(
        ICommandHandler<RecordProgressCommand> recordProgress,
        IQueryHandler<GetUserProgressQuery, IReadOnlyList<LearnerProgressDto>> getUserProgress,
        ILogger<ProgressController> logger)
    {
        _recordProgress = recordProgress;
        _getUserProgress = getUserProgress;
        _logger = logger;
    }

    /// <summary>Records (or updates) the authenticated user's progress on a lesson.</summary>
    [HttpPost]
    public async Task<IActionResult> RecordProgress([FromBody] RecordProgressRequest request, CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result result = await _recordProgress.HandleAsync(
            new RecordProgressCommand(userId, request.LessonId, request.IsCompleted, request.ScorePercent), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("RecordProgress succeeded: UserId={UserId}, LessonId={LessonId}", userId, request.LessonId);
        return NoContent();
    }

    /// <summary>Gets the authenticated user's progress across all lessons.</summary>
    [HttpGet]
    public async Task<IActionResult> GetUserProgress(CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result<IReadOnlyList<LearnerProgressDto>> result = await _getUserProgress.HandleAsync(new GetUserProgressQuery(userId), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        return Ok(result.Value);
    }
}
