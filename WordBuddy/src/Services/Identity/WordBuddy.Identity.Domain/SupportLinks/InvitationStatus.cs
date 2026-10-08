namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>Status of a <see cref="SupportLinkInvitation"/>. Expiry is checked by time, not stored.</summary>
public enum InvitationStatus
{
    Pending,
    Accepted,
    Cancelled
}
