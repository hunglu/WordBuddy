using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetOwnedGroups;

/// <summary>Not cached. Member counts are read for the groups of the page only.</summary>
public sealed class GetOwnedGroupsQueryHandler : IQueryHandler<GetOwnedGroupsQuery, PagedResult<OwnedGroupDto>>
{
    private readonly ILearnerGroupRepository _groups;
    private readonly IValidator<GetOwnedGroupsQuery> _validator;
    private readonly ILogger<GetOwnedGroupsQueryHandler> _logger;

    public GetOwnedGroupsQueryHandler(
        ILearnerGroupRepository groups,
        IValidator<GetOwnedGroupsQuery> validator,
        ILogger<GetOwnedGroupsQueryHandler> logger)
    {
        _groups = groups;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<OwnedGroupDto>>> HandleAsync(GetOwnedGroupsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetOwnedGroupsQuery started: CallerId={CallerId}, Page={Page}, PageSize={PageSize}", query.CallerId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetOwnedGroupsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<OwnedGroupDto>>(Error.Validation("GetOwnedGroups.Validation", validation.ToString()));
        }

        PagedResult<LearnerGroup> owned = await _groups.GetGroupsByOwnerPagedAsync(query.CallerId, query.Page, query.PageSize, ct);
        IReadOnlyList<LearnerGroupMember> members = await _groups.GetOpenMembersAsync(owned.Items.Select(g => g.Id).ToList(), ct);
        ILookup<Guid, LearnerGroupMember> membersByGroup = members.ToLookup(m => m.GroupId);

        List<OwnedGroupDto> items = owned.Items
            .Select(g => new OwnedGroupDto(
                g.Id,
                g.Name,
                membersByGroup[g.Id].Count(m => m.IsActive),
                membersByGroup[g.Id].Count(m => m.Status == GroupMemberStatus.PendingPrimaryApproval),
                g.CreatedAtUtc))
            .ToList();

        _logger.LogInformation("GetOwnedGroupsQuery succeeded: CallerId={CallerId}, Count={Count}, TotalCount={TotalCount}", query.CallerId, items.Count, owned.TotalCount);
        return Result.Success(new PagedResult<OwnedGroupDto>(items, owned.Page, owned.PageSize, owned.TotalCount));
    }
}
