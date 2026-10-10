namespace WordBuddy.Identity.Domain.Groups;

/// <summary>Lifecycle status of a <see cref="LearnerGroupMember"/>.</summary>
public enum GroupMemberStatus
{
    /// <summary>A child member waits for the Primary supporter of the child to approve.</summary>
    PendingPrimaryApproval,

    /// <summary>Counts as a member. Content and Progress see the member.</summary>
    Active,

    /// <summary>Ended. Final state.</summary>
    Removed
}
