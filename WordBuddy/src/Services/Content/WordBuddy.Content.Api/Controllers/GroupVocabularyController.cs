using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Authorization;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Groups.Commands.AssignWordsToGroup;
using WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>
/// Word lists for learning groups. The caller must own the group (checked in the handler against the
/// group projection: a non-owner gets 403, a child is never an owner). Logs ids and counts only.
/// </summary>
[ApiController]
[Route("api/vocabulary/groups/{groupId:guid}")]
[Authorize]
[Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
public sealed class GroupVocabularyController : ControllerBase
{
    private readonly ICommandHandler<AssignWordsToGroupCommand, GroupWordAssignmentResultDto> _assign;
    private readonly IQueryHandler<GetGroupWordAssignmentsQuery, IReadOnlyList<GroupWordAssignmentDto>> _getAssignments;

    public GroupVocabularyController(
        ICommandHandler<AssignWordsToGroupCommand, GroupWordAssignmentResultDto> assign,
        IQueryHandler<GetGroupWordAssignmentsQuery, IReadOnlyList<GroupWordAssignmentDto>> getAssignments)
    {
        _assign = assign;
        _getAssignments = getAssignments;
    }

    /// <summary>Adds the senses to the list of every active member. Returns counts of added, already had and skipped.</summary>
    [HttpPost("words")]
    public async Task<IActionResult> AssignWords(Guid groupId, [FromBody] AssignWordsToGroupRequest request, CancellationToken ct)
    {
        Result<GroupWordAssignmentResultDto> result = await _assign.HandleAsync(
            new AssignWordsToGroupCommand(groupId, User.GetUserId(), request.SenseIds ?? []), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Returns the assignment history of the group, newest first.</summary>
    [HttpGet("words")]
    public async Task<IActionResult> GetAssignments(Guid groupId, CancellationToken ct)
    {
        Result<IReadOnlyList<GroupWordAssignmentDto>> result =
            await _getAssignments.HandleAsync(new GetGroupWordAssignmentsQuery(groupId, User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }
}
