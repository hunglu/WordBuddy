namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>Status of an <see cref="UnlinkRequest"/>. Only <see cref="Pending"/> and <see cref="OverrideRequested"/> are open.</summary>
public enum UnlinkRequestStatus
{
    Pending,
    OverrideRequested,
    Confirmed,
    Declined,
    Cancelled,
    CompletedByAdmin,
    RejectedByAdmin
}
