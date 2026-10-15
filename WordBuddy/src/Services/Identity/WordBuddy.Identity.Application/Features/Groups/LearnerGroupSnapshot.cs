using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Groups;

/// <summary>
/// A group with all its pending and active members (at most the group member limit). It is the
/// cached unit under <c>identity:group-snapshot:{groupId}</c>; the group detail and the members page
/// are both cut from it.
/// </summary>
/// <param name="Id">Group id.</param>
/// <param name="OwnerId">Owner user id.</param>
/// <param name="Name">Group name.</param>
/// <param name="CreatedAtUtc">Creation time.</param>
/// <param name="Members">Pending and active members, oldest first.</param>
public sealed record LearnerGroupSnapshot(
    Guid Id,
    Guid OwnerId,
    string Name,
    DateTime CreatedAtUtc,
    IReadOnlyList<GroupMemberDto> Members);
