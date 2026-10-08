namespace WordBuddy.Content.Domain;

/// <summary>
/// Local copy of an Identity support link (ids and state only), kept by the link events.
/// Used by the <c>CanSupportLearner</c> and <c>ChildHasSupporter</c> policies.
/// </summary>
public sealed class SupportLinkProjection
{
    /// <summary>Gets the Identity link id (primary key).</summary>
    public Guid LinkId { get; private set; }

    /// <summary>Gets the learner.</summary>
    public Guid LearnerId { get; private set; }

    /// <summary>Gets the supporter.</summary>
    public Guid SupporterId { get; private set; }

    /// <summary>Gets a value indicating whether the link is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the time of the last applied event, UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private SupportLinkProjection()
    {
    }

    /// <summary>Creates the projection from the first event seen.</summary>
    public static SupportLinkProjection Create(Guid linkId, Guid learnerId, Guid supporterId, bool isActive, DateTime occurredAtUtc) =>
        new()
        {
            LinkId = linkId,
            LearnerId = learnerId,
            SupporterId = supporterId,
            IsActive = isActive,
            UpdatedAtUtc = occurredAtUtc,
        };

    /// <summary>
    /// Applies an event. Returns <see langword="false"/> (no change) for an event older than the
    /// last applied one, or a replay of the same state at the same time.
    /// </summary>
    public bool Apply(bool isActive, DateTime occurredAtUtc)
    {
        if (occurredAtUtc < UpdatedAtUtc || (occurredAtUtc == UpdatedAtUtc && isActive == IsActive))
        {
            return false;
        }

        IsActive = isActive;
        UpdatedAtUtc = occurredAtUtc;
        return true;
    }
}
