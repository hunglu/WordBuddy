using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Lessons — vocabulary, grammar, and daily-phrase content.</summary>
[ApiController]
[Route("api/lessons")]
[Authorize]
public sealed class LessonsController : ControllerBase
{
    private readonly IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>> _getLessons;
    private readonly IQueryHandler<GetLessonDetailQuery, LessonDetailDto> _getLessonDetail;
    private readonly ICommandHandler<CreateLessonCommand, Guid> _createLesson;
    private readonly ILogger<LessonsController> _logger;

    public LessonsController(
        IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>> getLessons,
        IQueryHandler<GetLessonDetailQuery, LessonDetailDto> getLessonDetail,
        ICommandHandler<CreateLessonCommand, Guid> createLesson,
        ILogger<LessonsController> logger)
    {
        _getLessons = getLessons;
        _getLessonDetail = getLessonDetail;
        _createLesson = createLesson;
        _logger = logger;
    }

    /// <summary>Lists published lessons, optionally filtered by type and/or level.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLessons([FromQuery] LessonType? type, [FromQuery] Level? level, CancellationToken ct)
    {
        Result<IReadOnlyList<LessonDto>> result = await _getLessons.HandleAsync(new GetLessonsQuery(type, level), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("GetLessons succeeded: Count={Count}", result.Value.Count);
        return Ok(result.Value);
    }

    /// <summary>Gets a single lesson with its full content.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLessonDetail(Guid id, CancellationToken ct)
    {
        Result<LessonDetailDto> result = await _getLessonDetail.HandleAsync(new GetLessonDetailQuery(id), ct);
        if (result.IsFailure)
        {
            _logger.LogWarning("GetLessonDetail not found: LessonId={LessonId}", id);
            return result.ToProblemResult(this);
        }

        return Ok(result.Value);
    }

    /// <summary>Creates a new lesson (admin use).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonCommand command, CancellationToken ct)
    {
        Result<Guid> result = await _createLesson.HandleAsync(command, ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("CreateLesson succeeded: LessonId={LessonId}", result.Value);
        return CreatedAtAction(nameof(GetLessonDetail), new { id = result.Value }, result.Value);
    }
}
