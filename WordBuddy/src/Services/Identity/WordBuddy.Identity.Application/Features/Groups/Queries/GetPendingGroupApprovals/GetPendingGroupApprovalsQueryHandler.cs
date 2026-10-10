using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;

public sealed class GetPendingGroupApprovalsQueryHandler : IQueryHandler<GetPendingGroupApprovalsQuery, IReadOnlyList<PendingGroupApprovalDto>>
{
    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly ILogger<GetPendingGroupApprovalsQueryHandler> _logger;

    public GetPendingGroupApprovalsQueryHandler(
        IUserRepository users,
        ILearnerGroupRepository groups,
        ILogger<GetPendingGroupApprovalsQueryHandler> logger)
    {
        _users = users;
        _groups = groups;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PendingGroupApprovalDto>>> HandleAsync(GetPendingGroupApprovalsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetPendingGroupApprovalsQuery started: CallerId={CallerId}", query.CallerId);

        IReadOnlyList<LearnerGroupMember> pending = await _groups.GetPendingForPrimaryAsync(query.CallerId, ct);
        IReadOnlyList<LearnerGroup> groups = await _groups.GetGroupsByIdsAsync(pending.Select(m => m.GroupId).Distinct().ToList(), ct);
        Dictionary<Guid, LearnerGroup> groupsById = groups.ToDictionary(g => g.Id);

        List<Guid> userIds = pending.Select(m => m.LearnerId).Concat(groups.Select(g => g.OwnerId)).Distinct().ToList();
        IReadOnlyList<User> users = await _users.GetByIdsAsync(userIds, ct);
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);

        List<PendingGroupApprovalDto> result = [];
        foreach (LearnerGroupMember member in pending)
        {
            if (!groupsById.TryGetValue(member.GroupId, out LearnerGroup? group) || !usersById.TryGetValue(member.LearnerId, out User? learner))
            {
                continue;
            }

            usersById.TryGetValue(group.OwnerId, out User? owner);
            result.Add(new PendingGroupApprovalDto(
                member.Id, group.Id, group.Name, owner?.PublicName ?? "Supporter", learner.Id, learner.PublicName, learner.AvatarId, member.AddedAtUtc));
        }

        _logger.LogInformation("GetPendingGroupApprovalsQuery succeeded: Count={Count}", result.Count);
        return Result.Success<IReadOnlyList<PendingGroupApprovalDto>>(result);
    }
}
