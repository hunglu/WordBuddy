namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>Audited link actions.</summary>
public enum SupportLinkAuditAction
{
    LinkActivated,
    LinkPendingPrimaryApproval,
    PrimaryApproved,
    PrimaryRejected,
    UnlinkRequested,
    UnlinkConfirmed,
    UnlinkDeclined,
    UnlinkCancelled,
    UnlinkEscalated,
    AdminUnlinkCompleted,
    AdminUnlinkRejected,
    AdminPrimaryHandover
}
