using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Groups.Commands.AddGroupMembers;
using WordBuddy.Identity.Application.Features.Groups.Commands.CreateGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.DeleteGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.LeaveGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.RemoveGroupMember;
using WordBuddy.Identity.Application.Features.Groups.Commands.RenameGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.RespondToGroupMembership;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetGroup;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetMyGroups;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>
/// Learning groups. A supporter owns groups and adds learners they actively support; a child member
/// needs the approval of the child's Primary supporter. Owner and Primary checks run in the handlers
/// (403). Child accounts get 403 on every owner action. Logs ids only, never names.
/// </summary>
[ApiController]
[Route("api/auth/groups")]
[Authorize]
public sealed class GroupsController : ControllerBase
{
    private readonly IQueryHandler<GetMyGroupsQuery, MyGroupsDto> _getMine;
    private readonly IQueryHandler<GetGroupQuery, LearnerGroupDetailDto> _getGroup;
    private readonly IQueryHandler<GetPendingGroupApprovalsQuery, IReadOnlyList<PendingGroupApprovalDto>> _getApprovals;
    private readonly ICommandHandler<CreateGroupCommand, Guid> _create;
    private readonly ICommandHandler<RenameGroupCommand> _rename;
    private readonly ICommandHandler<DeleteGroupCommand> _delete;
    private readonly ICommandHandler<AddGroupMembersCommand, IReadOnlyList<AddMemberResultDto>> _addMembers;
    private readonly ICommandHandler<RemoveGroupMemberCommand> _removeMember;
    private readonly ICommandHandler<LeaveGroupCommand> _leave;
    private readonly ICommandHandler<RespondToGroupMembershipCommand> _respond;

    public GroupsController(
        IQueryHandler<GetMyGroupsQuery, MyGroupsDto> getMine,
        IQueryHandler<GetGroupQuery, LearnerGroupDetailDto> getGroup,
        IQueryHandler<GetPendingGroupApprovalsQuery, IReadOnlyList<PendingGroupApprovalDto>> getApprovals,
        ICommandHandler<CreateGroupCommand, Guid> create,
        ICommandHandler<RenameGroupCommand> rename,
        ICommandHandler<DeleteGroupCommand> delete,
        ICommandHandler<AddGroupMembersCommand, IReadOnlyList<AddMemberResultDto>> addMembers,
        ICommandHandler<RemoveGroupMemberCommand> removeMember,
        ICommandHandler<LeaveGroupCommand> leave,
        ICommandHandler<RespondToGroupMembershipCommand> respond)
    {
        _getMine = getMine;
        _getGroup = getGroup;
        _getApprovals = getApprovals;
        _create = create;
        _rename = rename;
        _delete = delete;
        _addMembers = addMembers;
        _removeMember = removeMember;
        _leave = leave;
        _respond = respond;
    }

    /// <summary>Returns groups the caller owns and groups the caller joined (name and owner only).</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        Result<MyGroupsDto> result = await _getMine.HandleAsync(new GetMyGroupsQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Returns pending child memberships waiting for the caller (as Primary supporter).</summary>
    [HttpGet("approvals")]
    public async Task<IActionResult> GetApprovals(CancellationToken ct)
    {
        Result<IReadOnlyList<PendingGroupApprovalDto>> result =
            await _getApprovals.HandleAsync(new GetPendingGroupApprovalsQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Returns a group with its members. Owner only.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetGroup(Guid id, CancellationToken ct)
    {
        Result<LearnerGroupDetailDto> result = await _getGroup.HandleAsync(new GetGroupQuery(id, User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Creates a group.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitingConfiguration.GroupWritePolicy)]
    public async Task<IActionResult> Create([FromBody] GroupNameRequest request, CancellationToken ct)
    {
        Result<Guid> result = await _create.HandleAsync(new CreateGroupCommand(User.GetUserId(), request.Name), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetGroup), new { id = result.Value }, new { id = result.Value })
            : result.ToProblemResult(this);
    }

    /// <summary>Renames a group. Owner only.</summary>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting(RateLimitingConfiguration.GroupWritePolicy)]
    public async Task<IActionResult> Rename(Guid id, [FromBody] GroupNameRequest request, CancellationToken ct)
    {
        Result result = await _rename.HandleAsync(new RenameGroupCommand(id, User.GetUserId(), request.Name), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Deletes a group and removes its members. Owner only.</summary>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitingConfiguration.GroupWritePolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        Result result = await _delete.HandleAsync(new DeleteGroupCommand(id, User.GetUserId()), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Adds learners the caller actively supports. Returns one outcome per learner.</summary>
    [HttpPost("{id:guid}/members")]
    [EnableRateLimiting(RateLimitingConfiguration.GroupWritePolicy)]
    public async Task<IActionResult> AddMembers(Guid id, [FromBody] AddGroupMembersRequest request, CancellationToken ct)
    {
        Result<IReadOnlyList<AddMemberResultDto>> result = await _addMembers.HandleAsync(
            new AddGroupMembersCommand(id, User.GetUserId(), request.LearnerIds ?? []), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Removes a member. Owner only.</summary>
    [HttpDelete("{id:guid}/members/{learnerId:guid}")]
    [EnableRateLimiting(RateLimitingConfiguration.GroupWritePolicy)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid learnerId, CancellationToken ct)
    {
        Result result = await _removeMember.HandleAsync(new RemoveGroupMemberCommand(id, User.GetUserId(), learnerId), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>An adult learner leaves a group. A child gets 403.</summary>
    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        Result result = await _leave.HandleAsync(new LeaveGroupCommand(id, User.GetUserId()), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Primary supporter approves a pending child membership.</summary>
    [HttpPost("approvals/{memberId:guid}/approve")]
    public Task<IActionResult> Approve(Guid memberId, CancellationToken ct) => Respond(memberId, approve: true, ct);

    /// <summary>Primary supporter rejects a pending child membership.</summary>
    [HttpPost("approvals/{memberId:guid}/reject")]
    public Task<IActionResult> Reject(Guid memberId, CancellationToken ct) => Respond(memberId, approve: false, ct);

    private async Task<IActionResult> Respond(Guid memberId, bool approve, CancellationToken ct)
    {
        Result result = await _respond.HandleAsync(new RespondToGroupMembershipCommand(memberId, User.GetUserId(), approve), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }
}
