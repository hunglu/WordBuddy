using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.API.Models.Requests;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Application.Features.Progress.Queries.GetUserProgress;
using WordBuddy.Domain.Common;

namespace WordBuddy.API.Controllers;

/// <summary>Provides endpoints for recording and retrieving learner progress.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProgressController : ControllerBase
{
    private readonly RecordProgressCommandHandler _recordProgressHandler;
    private readonly GetUserProgressQueryHandler _getUserProgressHandler;

    /// <summary>Initializes a new <see cref="ProgressController"/>.</summary>
    public ProgressController(
        RecordProgressCommandHandler recordProgressHandler,
        GetUserProgressQueryHandler getUserProgressHandler)
    {
        _recordProgressHandler = recordProgressHandler;
        _getUserProgressHandler = getUserProgressHandler;
    }

    /// <summary>Records or updates the authenticated user's progress for a lesson.</summary>
    /// <param name="request">The progress details to record.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordProgress([FromBody] RecordProgressRequest request, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();

        Result result = await _recordProgressHandler.HandleAsync(
            new RecordProgressCommand(userId, request.LessonId, request.IsCompleted, request.ScorePercent),
            ct);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    /// <summary>Returns all lesson progress records for the authenticated user.</summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType<List<LearnerProgressDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProgress(CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();

        Result<List<LearnerProgressDto>> result = await _getUserProgressHandler.HandleAsync(
            new GetUserProgressQuery(userId), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    private Guid GetCurrentUserId()
    {
        string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out Guid id) ? id : Guid.Empty;
    }
}
