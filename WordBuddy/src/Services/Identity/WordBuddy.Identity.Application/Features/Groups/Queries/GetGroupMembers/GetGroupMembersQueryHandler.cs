using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroupMembers;

/// <summary>
/// Cuts one page of members from the cached group snapshot. The owner check runs on every call,
/// including cache hits.
/// </summary>
public sealed class GetGroupMembersQueryHandler : IQueryHandler<GetGroupMembersQuery, PagedResult<GroupMemberDto>>
{
    private readonly LearnerGroupSnapshotLoader _snapshots;
    private readonly IValidator<GetGroupMembersQuery> _validator;
    private readonly ILogger<GetGroupMembersQueryHandler> _logger;

    public GetGroupMembersQueryHandler(
        LearnerGroupSnapshotLoader snapshots,
        IValidator<GetGroupMembersQuery> validator,
        ILogger<GetGroupMembersQueryHandler> logger)
    {
        _snapshots = snapshots;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<GroupMemberDto>>> HandleAsync(GetGroupMembersQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetGroupMembersQuery started: GroupId={GroupId}, CallerId={CallerId}, Page={Page}, PageSize={PageSize}",
            query.GroupId, query.CallerId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetGroupMembersQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<GroupMemberDto>>(Error.Validation("GetGroupMembers.Validation", validation.ToString()));
        }

        Result<LearnerGroupSnapshot> loaded = await _snapshots.LoadAsync(query.GroupId, ct);
        if (loaded.IsFailure)
        {
            return Result.Failure<PagedResult<GroupMemberDto>>(loaded.Error);
        }

        if (loaded.Value.OwnerId != query.CallerId)
        {
            _logger.LogWarning("GetGroupMembersQuery rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, LearnerGroupErrors.NotOwner.Code);
            return Result.Failure<PagedResult<GroupMemberDto>>(LearnerGroupErrors.NotOwner);
        }

        PagedResult<GroupMemberDto> page = Paging.Slice(loaded.Value.Members, query.Page, query.PageSize);
        _logger.LogInformation("GetGroupMembersQuery succeeded: GroupId={GroupId}, Count={Count}, TotalCount={TotalCount}", query.GroupId, page.Items.Count, page.TotalCount);
        return Result.Success(page);
    }
}
