using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetMyGroups;

/// <summary>
/// Not cached. The joined list never contains other members: a learner sees the group name and the
/// owner public name only.
/// </summary>
public sealed class GetMyGroupsQueryHandler : IQueryHandler<GetMyGroupsQuery, MyGroupsDto>
{
    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly ILogger<GetMyGroupsQueryHandler> _logger;

    public GetMyGroupsQueryHandler(IUserRepository users, ILearnerGroupRepository groups, ILogger<GetMyGroupsQueryHandler> logger)
    {
        _users = users;
        _groups = groups;
        _logger = logger;
    }

    public async Task<Result<MyGroupsDto>> HandleAsync(GetMyGroupsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetMyGroupsQuery started: CallerId={CallerId}", query.CallerId);

        IReadOnlyList<LearnerGroup> owned = await _groups.GetGroupsByOwnerAsync(query.CallerId, ct);
        IReadOnlyList<LearnerGroupMember> memberships = await _groups.GetActiveMembershipsOfLearnerAsync(query.CallerId, ct);
        IReadOnlyList<LearnerGroupMember> ownedMembers = await _groups.GetOpenMembersAsync(owned.Select(g => g.Id).ToList(), ct);
        ILookup<Guid, LearnerGroupMember> membersByGroup = ownedMembers.ToLookup(m => m.GroupId);

        IReadOnlyList<LearnerGroup> joinedGroups = await _groups.GetGroupsByIdsAsync(memberships.Select(m => m.GroupId).ToList(), ct);
        IReadOnlyList<User> owners = await _users.GetByIdsAsync(joinedGroups.Select(g => g.OwnerId).Distinct().ToList(), ct);
        Dictionary<Guid, User> ownersById = owners.ToDictionary(u => u.Id);

        MyGroupsDto result = new(
            owned.Select(g => new OwnedGroupDto(
                g.Id,
                g.Name,
                membersByGroup[g.Id].Count(m => m.IsActive),
                membersByGroup[g.Id].Count(m => m.Status == GroupMemberStatus.PendingPrimaryApproval),
                g.CreatedAtUtc)).ToList(),
            joinedGroups.Select(g =>
            {
                ownersById.TryGetValue(g.OwnerId, out User? owner);
                return new JoinedGroupDto(g.Id, g.Name, owner?.PublicName ?? "Supporter", owner?.AvatarId);
            }).ToList());

        _logger.LogInformation(
            "GetMyGroupsQuery succeeded: CallerId={CallerId}, Owned={Owned}, Joined={Joined}",
            query.CallerId, result.Owned.Count, result.Joined.Count);
        return Result.Success(result);
    }
}
