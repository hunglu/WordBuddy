namespace WordBuddy.Identity.Domain.Groups;

/// <summary>Why a <see cref="LearnerGroupMember"/> was removed.</summary>
public enum GroupMemberRemovedReason
{
    /// <summary>The group owner removed the member.</summary>
    ByOwner,

    /// <summary>The support link between owner and learner ended.</summary>
    LinkRevoked,

    /// <summary>The adult learner left.</summary>
    Left,

    /// <summary>The Primary supporter of the child rejected the membership.</summary>
    RejectedByPrimary,

    /// <summary>The group was deleted.</summary>
    GroupDeleted
}
