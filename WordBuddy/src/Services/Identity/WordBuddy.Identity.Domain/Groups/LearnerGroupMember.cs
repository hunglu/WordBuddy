using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.Groups;

/// <summary>
/// A learner in a group. Transitions: PendingPrimaryApproval to Active (approve) or Removed
/// (reject, remove); Active to Removed. Removed is final. Create through <see cref="LearnerGroupPolicy"/>.
/// </summary>
public sealed class LearnerGroupMember : Entity
{
    /// <summary>Gets the group.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Gets the learner.</summary>
    public Guid LearnerId { get; private set; }

    /// <summary>Gets the status.</summary>
    public GroupMemberStatus Status { get; private set; }

    /// <summary>Gets when the learner was added, UTC.</summary>
    public DateTime AddedAtUtc { get; private set; }

    /// <summary>Gets when the membership last changed, UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Gets why the member was removed. Null unless removed.</summary>
    public GroupMemberRemovedReason? RemovedReason { get; private set; }

    /// <summary>Gets a value indicating whether the member is active.</summary>
    public bool IsActive => Status == GroupMemberStatus.Active;

    private LearnerGroupMember(Guid id) : base(id)
    {
    }

    internal static LearnerGroupMember Create(Guid id, Guid groupId, Guid learnerId, GroupMemberStatus status, DateTime nowUtc) =>
        new(id)
        {
            GroupId = groupId,
            LearnerId = learnerId,
            Status = status,
            AddedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };

    /// <summary>The Primary supporter approves a pending child.</summary>
    public Result Approve(DateTime nowUtc)
    {
        if (Status != GroupMemberStatus.PendingPrimaryApproval)
        {
            return Result.Failure(LearnerGroupErrors.InvalidStatus);
        }

        Status = GroupMemberStatus.Active;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>The Primary supporter rejects a pending child.</summary>
    public Result Reject(DateTime nowUtc)
    {
        if (Status != GroupMemberStatus.PendingPrimaryApproval)
        {
            return Result.Failure(LearnerGroupErrors.InvalidStatus);
        }

        return Remove(GroupMemberRemovedReason.RejectedByPrimary, nowUtc);
    }

    /// <summary>Ends a pending or active membership.</summary>
    public Result Remove(GroupMemberRemovedReason reason, DateTime nowUtc)
    {
        if (Status == GroupMemberStatus.Removed)
        {
            return Result.Failure(LearnerGroupErrors.InvalidStatus);
        }

        Status = GroupMemberStatus.Removed;
        RemovedReason = reason;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }
}
