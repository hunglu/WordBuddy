using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Application.Features.Groups;

/// <inheritdoc />
public sealed class LearnerGroupMembershipCleaner : ILearnerGroupMembershipCleaner
{
    private readonly ILearnerGroupRepository _groups;
    private readonly ILearnerGroupEventPublisher _events;
    private readonly ILogger<LearnerGroupMembershipCleaner> _logger;

    public LearnerGroupMembershipCleaner(
        ILearnerGroupRepository groups,
        ILearnerGroupEventPublisher events,
        ILogger<LearnerGroupMembershipCleaner> logger)
    {
        _groups = groups;
        _events = events;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> RemoveForLinkAsync(Guid learnerId, Guid supporterId, DateTime nowUtc, CancellationToken ct = default)
    {
        IReadOnlyList<LearnerGroupMember> members =
            await _groups.GetOpenMembersOfOwnerAndLearnerTrackedAsync(supporterId, learnerId, ct);

        List<Guid> groupIds = [];
        foreach (LearnerGroupMember member in members)
        {
            bool wasActive = member.IsActive;
            if (member.Remove(GroupMemberRemovedReason.LinkRevoked, nowUtc).IsFailure)
            {
                continue;
            }

            groupIds.Add(member.GroupId);
            if (wasActive)
            {
                await _events.PublishMemberRemovedAsync(
                    member.GroupId, supporterId, learnerId, GroupMemberRemovedReason.LinkRevoked, nowUtc, ct);
            }
        }

        _logger.LogInformation(
            "Group memberships removed after link end: LearnerId={LearnerId}, SupporterId={SupporterId}, Count={Count}",
            learnerId, supporterId, groupIds.Count);
        return groupIds;
    }
}
