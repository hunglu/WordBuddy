using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Groups;

/// <summary>Typed errors of the group dashboard. A code ending in <c>.Forbidden</c> maps to HTTP 403.</summary>
public static class GroupDashboardErrors
{
    public static readonly Error Forbidden = Error.Failure("GroupDashboard.Forbidden", "Only the group owner can do this.");
}

/// <summary>
/// Decision behind the <c>CanManageGroup</c> policy, on the local
/// <see cref="Domain.LearnerGroupMemberProjection"/>. The Api authorization handler only reads claims and calls this.
/// </summary>
public sealed class GroupAccess
{
    private readonly ILearnerGroupProjectionRepository _groups;

    public GroupAccess(ILearnerGroupProjectionRepository groups)
    {
        _groups = groups;
    }

    /// <summary>True when <paramref name="callerId"/> owns <paramref name="groupId"/>. Unknown group or missing ids → false.</summary>
    public async Task<bool> CanManageGroupAsync(Guid? callerId, Guid? groupId, CancellationToken ct = default) =>
        callerId is { } caller && groupId is { } group && await _groups.IsOwnerAsync(group, caller, ct);
}
