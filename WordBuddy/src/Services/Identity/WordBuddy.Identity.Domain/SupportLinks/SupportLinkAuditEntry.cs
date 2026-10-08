using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>
/// Insert-only audit row for a link action. No update method exists on purpose.
/// Holds ids, action, time and an optional admin reason only — no names, emails or child data.
/// </summary>
public sealed class SupportLinkAuditEntry : Entity
{
    /// <summary>Maximum length of <see cref="Reason"/>.</summary>
    public const int ReasonMaxLength = 500;

    /// <summary>Gets the link.</summary>
    public Guid LinkId { get; }

    /// <summary>Gets the acting user.</summary>
    public Guid ActorId { get; }

    /// <summary>Gets the action.</summary>
    public SupportLinkAuditAction Action { get; }

    /// <summary>Gets when the action happened, UTC.</summary>
    public DateTime AtUtc { get; }

    /// <summary>Gets the admin reason, if any.</summary>
    public string? Reason { get; }

    /// <summary>Creates an audit row.</summary>
    public SupportLinkAuditEntry(Guid id, Guid linkId, Guid actorId, SupportLinkAuditAction action, DateTime atUtc, string? reason = null)
        : base(id)
    {
        LinkId = linkId;
        ActorId = actorId;
        Action = action;
        AtUtc = atUtc;
        Reason = reason;
    }
}
