using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetJoinedGroups;

/// <summary>Not cached. The joined list never contains other members: a learner sees the group name and the
/// owner public name only.</summary>
public sealed class GetJoinedGroupsQueryHandler : IQueryHandler<GetJoinedGroupsQuery, PagedResult<JoinedGroupDto>>
{
    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly IValidator<GetJoinedGroupsQuery> _validator;
    private readonly ILogger<GetJoinedGroupsQueryHandler> _logger;

    public GetJoinedGroupsQueryHandler(
        IUserRepository users,
        ILearnerGroupRepository groups,
        IValidator<GetJoinedGroupsQuery> validator,
        ILogger<GetJoinedGroupsQueryHandler> logger)
    {
        _users = users;
        _groups = groups;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<JoinedGroupDto>>> HandleAsync(GetJoinedGroupsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetJoinedGroupsQuery started: CallerId={CallerId}, Page={Page}, PageSize={PageSize}", query.CallerId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetJoinedGroupsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<JoinedGroupDto>>(Error.Validation("GetJoinedGroups.Validation", validation.ToString()));
        }

        PagedResult<LearnerGroup> joined = await _groups.GetJoinedGroupsPagedAsync(query.CallerId, query.Page, query.PageSize, ct);
        IReadOnlyList<User> owners = await _users.GetByIdsAsync(joined.Items.Select(g => g.OwnerId).Distinct().ToList(), ct);
        Dictionary<Guid, User> ownersById = owners.ToDictionary(u => u.Id);

        List<JoinedGroupDto> items = joined.Items
            .Select(g =>
            {
                ownersById.TryGetValue(g.OwnerId, out User? owner);
                return new JoinedGroupDto(g.Id, g.Name, owner?.PublicName ?? "Supporter", owner?.AvatarId);
            })
            .ToList();

        _logger.LogInformation("GetJoinedGroupsQuery succeeded: CallerId={CallerId}, Count={Count}, TotalCount={TotalCount}", query.CallerId, items.Count, joined.TotalCount);
        return Result.Success(new PagedResult<JoinedGroupDto>(items, joined.Page, joined.PageSize, joined.TotalCount));
    }
}
