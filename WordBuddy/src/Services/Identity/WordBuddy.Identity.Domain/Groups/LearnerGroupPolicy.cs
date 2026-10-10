using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.Groups;

/// <summary>
/// Rules for adding a learner to a group: the owner is an adult with an active support link to the
/// learner; no duplicate membership; limits hold. An adult learner is active at once. A child is
/// pending until the Primary supporter approves, unless the owner is the Primary of that child.
/// </summary>
public static class LearnerGroupPolicy
{
    /// <summary>Checks whether the owner may create one more group.</summary>
    /// <param name="owner">The caller.</param>
    /// <param name="ownerGroupCount">Groups the owner has today (not deleted).</param>
    /// <param name="maxGroupsPerOwner">The configured limit.</param>
    public static Result CanCreateGroup(LinkParty owner, int ownerGroupCount, int maxGroupsPerOwner)
    {
        if (owner.IsChild)
        {
            return Result.Failure(LearnerGroupErrors.ChildForbidden);
        }

        return ownerGroupCount >= maxGroupsPerOwner ? Result.Failure(LearnerGroupErrors.TooManyGroups) : Result.Success();
    }

    /// <summary>Creates a member row for <paramref name="learner"/>, or returns the reason it is not allowed.</summary>
    /// <param name="memberId">New member id.</param>
    /// <param name="group">The group.</param>
    /// <param name="owner">The caller (must be the group owner).</param>
    /// <param name="learner">The learner to add.</param>
    /// <param name="ownerHasActiveLink">Whether the owner has an active support link to the learner.</param>
    /// <param name="ownerIsLearnerPrimary">Whether the owner is the active Primary supporter of the learner.</param>
    /// <param name="learnerIsAlreadyMember">Whether the learner is already pending or active in the group.</param>
    /// <param name="currentMemberCount">Pending plus active members of the group.</param>
    /// <param name="maxMembersPerGroup">The configured limit.</param>
    /// <param name="nowUtc">Current time.</param>
    public static Result<LearnerGroupMember> AddMember(
        Guid memberId,
        LearnerGroup group,
        LinkParty owner,
        LinkParty learner,
        bool ownerHasActiveLink,
        bool ownerIsLearnerPrimary,
        bool learnerIsAlreadyMember,
        int currentMemberCount,
        int maxMembersPerGroup,
        DateTime nowUtc)
    {
        if (owner.IsChild)
        {
            return Result.Failure<LearnerGroupMember>(LearnerGroupErrors.ChildForbidden);
        }

        if (owner.UserId != group.OwnerId)
        {
            return Result.Failure<LearnerGroupMember>(LearnerGroupErrors.NotOwner);
        }

        if (owner.UserId == learner.UserId)
        {
            return Result.Failure<LearnerGroupMember>(LearnerGroupErrors.SelfAdd);
        }

        if (!ownerHasActiveLink)
        {
            return Result.Failure<LearnerGroupMember>(LearnerGroupErrors.NoActiveLink);
        }

        if (learnerIsAlreadyMember)
        {
            return Result.Failure<LearnerGroupMember>(LearnerGroupErrors.AlreadyMember);
        }

        if (currentMemberCount >= maxMembersPerGroup)
        {
            return Result.Failure<LearnerGroupMember>(LearnerGroupErrors.GroupFull);
        }

        GroupMemberStatus status = learner.IsChild && !ownerIsLearnerPrimary
            ? GroupMemberStatus.PendingPrimaryApproval
            : GroupMemberStatus.Active;

        return Result.Success(LearnerGroupMember.Create(memberId, group.Id, learner.UserId, status, nowUtc));
    }

    /// <summary>Checks that <paramref name="actor"/> may approve or reject a pending child member.</summary>
    /// <param name="member">The member.</param>
    /// <param name="actor">The calling user.</param>
    /// <param name="learnerPrimarySupporterId">The active Primary supporter of the member learner, if any.</param>
    public static Result CanRespond(LearnerGroupMember member, LinkParty actor, Guid? learnerPrimarySupporterId)
    {
        if (actor.IsChild)
        {
            return Result.Failure(LearnerGroupErrors.ChildForbidden);
        }

        if (learnerPrimarySupporterId != actor.UserId)
        {
            return Result.Failure(LearnerGroupErrors.NotPrimary);
        }

        return member.Status == GroupMemberStatus.PendingPrimaryApproval
            ? Result.Success()
            : Result.Failure(LearnerGroupErrors.InvalidStatus);
    }

    /// <summary>Checks that <paramref name="learner"/> may leave: an adult member can, a child cannot.</summary>
    public static Result CanLeave(LearnerGroupMember member, LinkParty learner)
    {
        if (learner.IsChild)
        {
            return Result.Failure(LearnerGroupErrors.ChildCannotLeave);
        }

        return member.LearnerId != learner.UserId || member.Status == GroupMemberStatus.Removed
            ? Result.Failure(LearnerGroupErrors.MemberNotFound)
            : Result.Success();
    }
}
