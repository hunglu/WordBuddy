namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>Lifecycle status of a <see cref="SupportLink"/>.</summary>
public enum SupportLinkStatus
{
    /// <summary>Open invitation, no supporter yet. Tracked on <see cref="SupportLinkInvitation"/>; a link row never has this status.</summary>
    Invited,

    /// <summary>Extra supporter of a child; waits for the Primary to approve.</summary>
    PendingPrimaryApproval,

    /// <summary>The supporter has the full, fixed permission set.</summary>
    Active,

    /// <summary>Ended. Final state.</summary>
    Revoked
}
