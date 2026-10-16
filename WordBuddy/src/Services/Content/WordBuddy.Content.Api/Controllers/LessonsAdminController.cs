using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Lessons.Commands.AttachLessonSense;
using WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.DeleteLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.DetachLessonSense;
using WordBuddy.Content.Application.Features.Lessons.Commands.PublishLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.ReorderLessonSenses;
using WordBuddy.Content.Application.Features.Lessons.Commands.UnpublishLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.UpdateLesson;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;
using WordBuddy.Content.Application.Features.Lessons.Queries.SearchLessonSenses;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Admin-only lesson authoring: create, edit, publish, delete, and manage the words of a lesson.
/// Admins see drafts here; learners never do.</summary>
[ApiController]
[Route("api/lessons/admin")]
[Authorize(Policy = ServiceCollectionExtensions.ManageContentPolicy)]
public sealed class LessonsAdminController : ControllerBase
{
    private readonly IQueryHandler<GetAdminLessonsQuery, PagedResult<AdminLessonDto>> _getLessons;
    private readonly IQueryHandler<GetAdminLessonDetailQuery, AdminLessonDetailDto> _getLessonDetail;
    private readonly IQueryHandler<SearchLessonSensesQuery, IReadOnlyList<AdminSenseSearchResultDto>> _searchSenses;
    private readonly ICommandHandler<CreateLessonCommand, Guid> _createLesson;
    private readonly ICommandHandler<UpdateLessonCommand> _updateLesson;
    private readonly ICommandHandler<PublishLessonCommand> _publishLesson;
    private readonly ICommandHandler<UnpublishLessonCommand> _unpublishLesson;
    private readonly ICommandHandler<DeleteLessonCommand> _deleteLesson;
    private readonly ICommandHandler<AttachLessonSenseCommand> _attachSense;
    private readonly ICommandHandler<DetachLessonSenseCommand> _detachSense;
    private readonly ICommandHandler<ReorderLessonSensesCommand> _reorderSenses;
    private readonly ILogger<LessonsAdminController> _logger;

    public LessonsAdminController(
        IQueryHandler<GetAdminLessonsQuery, PagedResult<AdminLessonDto>> getLessons,
        IQueryHandler<GetAdminLessonDetailQuery, AdminLessonDetailDto> getLessonDetail,
        IQueryHandler<SearchLessonSensesQuery, IReadOnlyList<AdminSenseSearchResultDto>> searchSenses,
        ICommandHandler<CreateLessonCommand, Guid> createLesson,
        ICommandHandler<UpdateLessonCommand> updateLesson,
        ICommandHandler<PublishLessonCommand> publishLesson,
        ICommandHandler<UnpublishLessonCommand> unpublishLesson,
        ICommandHandler<DeleteLessonCommand> deleteLesson,
        ICommandHandler<AttachLessonSenseCommand> attachSense,
        ICommandHandler<DetachLessonSenseCommand> detachSense,
        ICommandHandler<ReorderLessonSensesCommand> reorderSenses,
        ILogger<LessonsAdminController> logger)
    {
        _getLessons = getLessons;
        _getLessonDetail = getLessonDetail;
        _searchSenses = searchSenses;
        _createLesson = createLesson;
        _updateLesson = updateLesson;
        _publishLesson = publishLesson;
        _unpublishLesson = unpublishLesson;
        _deleteLesson = deleteLesson;
        _attachSense = attachSense;
        _detachSense = detachSense;
        _reorderSenses = reorderSenses;
        _logger = logger;
    }

    /// <summary>Lists drafts and published lessons, optionally filtered.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLessons(
        [FromQuery] LessonType? type,
        [FromQuery] Level? level,
        [FromQuery] bool? isPublished,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken ct = default)
    {
        Result<PagedResult<AdminLessonDto>> result = await _getLessons.HandleAsync(
            new GetAdminLessonsQuery(type, level, isPublished, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Gets one lesson for the editor, drafts included, with every attached word.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLessonDetail(Guid id, CancellationToken ct)
    {
        Result<AdminLessonDetailDto> result = await _getLessonDetail.HandleAsync(new GetAdminLessonDetailQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Finds words that can be attached to a lesson (system words and shared-pool words).</summary>
    [HttpGet("senses")]
    public async Task<IActionResult> SearchSenses([FromQuery] string? search, CancellationToken ct)
    {
        Result<IReadOnlyList<AdminSenseSearchResultDto>> result = await _searchSenses.HandleAsync(new SearchLessonSensesQuery(search), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Creates a lesson as a draft.</summary>
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

    /// <summary>Changes the title, description, level and target age group of a lesson.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLesson(Guid id, [FromBody] UpdateLessonRequest request, CancellationToken ct) =>
        NoContentOrProblem(await _updateLesson.HandleAsync(
            new UpdateLessonCommand(id, request.Title, request.Description, request.Level, request.TargetAgeGroup), ct));

    /// <summary>Makes a lesson visible to learners.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishLesson(Guid id, CancellationToken ct) =>
        NoContentOrProblem(await _publishLesson.HandleAsync(new PublishLessonCommand(id), ct));

    /// <summary>Hides a lesson from learners.</summary>
    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> UnpublishLesson(Guid id, CancellationToken ct) =>
        NoContentOrProblem(await _unpublishLesson.HandleAsync(new UnpublishLessonCommand(id), ct));

    /// <summary>Deletes a lesson. Its words stay in the catalogue.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLesson(Guid id, CancellationToken ct) =>
        NoContentOrProblem(await _deleteLesson.HandleAsync(new DeleteLessonCommand(id), ct));

    /// <summary>Adds a word to the end of a vocabulary lesson.</summary>
    [HttpPost("{id:guid}/senses")]
    public async Task<IActionResult> AttachSense(Guid id, [FromBody] AttachLessonSenseRequest request, CancellationToken ct) =>
        NoContentOrProblem(await _attachSense.HandleAsync(new AttachLessonSenseCommand(id, request.SenseId), ct));

    /// <summary>Removes a word from a lesson.</summary>
    [HttpDelete("{id:guid}/senses/{senseId:guid}")]
    public async Task<IActionResult> DetachSense(Guid id, Guid senseId, CancellationToken ct) =>
        NoContentOrProblem(await _detachSense.HandleAsync(new DetachLessonSenseCommand(id, senseId), ct));

    /// <summary>Sets the order of the words of a lesson.</summary>
    [HttpPut("{id:guid}/senses/order")]
    public async Task<IActionResult> ReorderSenses(Guid id, [FromBody] ReorderLessonSensesRequest request, CancellationToken ct) =>
        NoContentOrProblem(await _reorderSenses.HandleAsync(new ReorderLessonSensesCommand(id, request.SenseIds ?? []), ct));

    private IActionResult NoContentOrProblem(Result result) => result.IsSuccess ? NoContent() : result.ToProblemResult(this);
}
