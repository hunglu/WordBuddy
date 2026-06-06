using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.API.Models.Requests;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Enums;

namespace WordBuddy.API.Controllers;

/// <summary>Provides endpoints for browsing and managing lessons.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LessonsController : ControllerBase
{
    private readonly GetLessonsQueryHandler _getLessonsHandler;
    private readonly GetLessonDetailQueryHandler _getLessonDetailHandler;
    private readonly CreateLessonCommandHandler _createLessonHandler;

    /// <summary>Initializes a new <see cref="LessonsController"/>.</summary>
    public LessonsController(
        GetLessonsQueryHandler getLessonsHandler,
        GetLessonDetailQueryHandler getLessonDetailHandler,
        CreateLessonCommandHandler createLessonHandler)
    {
        _getLessonsHandler = getLessonsHandler;
        _getLessonDetailHandler = getLessonDetailHandler;
        _createLessonHandler = createLessonHandler;
    }

    /// <summary>Returns published lessons, optionally filtered by type, level, and age group.</summary>
    /// <param name="type">Filter by lesson type (Vocabulary, Grammar, DailyPhrase).</param>
    /// <param name="level">Filter by difficulty level.</param>
    /// <param name="ageGroup">Filter by the caller's age group; the handler maps this to <c>TargetAgeGroup</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType<List<LessonDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLessons(
        [FromQuery] LessonType? type,
        [FromQuery] Level? level,
        [FromQuery] AgeGroup? ageGroup,
        CancellationToken ct)
    {
        Result<List<LessonDto>> result = await _getLessonsHandler.HandleAsync(
            new GetLessonsQuery(type, level, ageGroup), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>Returns the full detail of a single lesson, including vocabulary, grammar rules, and daily phrases.</summary>
    /// <param name="id">The lesson identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LessonDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLesson(Guid id, CancellationToken ct)
    {
        Result<LessonDetailDto> result = await _getLessonDetailHandler.HandleAsync(
            new GetLessonDetailQuery(id), ct);

        if (result.IsFailure)
            return result.Error.Code.Contains("NotFound") ? NotFound(result.Error) : BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>Creates a new lesson.</summary>
    /// <param name="request">The lesson details.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request, CancellationToken ct)
    {
        Result<Guid> result = await _createLessonHandler.HandleAsync(
            new CreateLessonCommand(
                request.Title, request.Description, request.Type,
                request.Level, request.TargetAgeGroup, request.OrderIndex),
            ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetLesson), new { id = result.Value }, new { id = result.Value })
            : BadRequest(result.Error);
    }
}
