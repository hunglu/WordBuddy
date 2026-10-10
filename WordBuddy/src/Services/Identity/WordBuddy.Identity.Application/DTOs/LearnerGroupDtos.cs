using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Application.DTOs;

/// <summary>A member of a group, shown to the owner. The name is the public name (alias first; a child without alias is "Child learner").</summary>
public sealed record GroupMemberDto(
    Guid MemberId,
    Guid LearnerId,
    string PublicName,
    string? AvatarId,
    AgeGroup AgeGroup,
    GroupMemberStatus Status,
    DateTime AddedAtUtc);

/// <summary>A group with its pending and active members. Owner view only.</summary>
public sealed record LearnerGroupDetailDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    DateTime CreatedAtUtc,
    IReadOnlyList<GroupMemberDto> Members);

/// <summary>A group the caller owns.</summary>
public sealed record OwnedGroupDto(Guid Id, string Name, int ActiveMemberCount, int PendingMemberCount, DateTime CreatedAtUtc);

/// <summary>A group the caller belongs to. Shows the group name and owner only, never other members.</summary>
public sealed record JoinedGroupDto(Guid Id, string Name, string OwnerName, string? OwnerAvatarId);

/// <summary>Groups the caller owns and groups the caller joined.</summary>
public sealed record MyGroupsDto(IReadOnlyList<OwnedGroupDto> Owned, IReadOnlyList<JoinedGroupDto> Joined);

/// <summary>Result of adding one learner to a group.</summary>
public enum AddMemberOutcome
{
    /// <summary>The learner is an active member.</summary>
    Added,

    /// <summary>The learner waits for the Primary supporter to approve.</summary>
    PendingApproval,

    /// <summary>The learner was not added. See <see cref="AddMemberResultDto.ErrorCode"/>.</summary>
    Rejected
}

/// <summary>Per-learner outcome of <c>AddGroupMembers</c>.</summary>
public sealed record AddMemberResultDto(Guid LearnerId, AddMemberOutcome Outcome, string? ErrorCode);

/// <summary>A pending child membership waiting for the caller (as Primary supporter).</summary>
public sealed record PendingGroupApprovalDto(
    Guid MemberId,
    Guid GroupId,
    string GroupName,
    string OwnerName,
    Guid LearnerId,
    string LearnerName,
    string? LearnerAvatarId,
    DateTime RequestedAtUtc);
