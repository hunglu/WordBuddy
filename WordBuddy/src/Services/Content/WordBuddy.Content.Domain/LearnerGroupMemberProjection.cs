namespace WordBuddy.Content.Domain;

/// <summary>
/// Local copy of an Identity group membership (ids and state only), kept by the group events.
/// A group has no row of its own: its owner is read from the member rows. Used to check that the
/// caller owns the group and to list the active members for a word assignment.
/// </summary>
public sealed class LearnerGroupMemberProjection
{
    /// <summary>Gets the Identity group id.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Gets the group owner (a supporter).</summary>
    public Guid OwnerId { get; private set; }

    /// <summary>Gets the learner.</summary>
    public Guid LearnerId { get; private set; }

    /// <summary>Gets a value indicating whether the learner is an active member.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the time of the last applied event, UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private LearnerGroupMemberProjection()
    {
    }

    /// <summary>Creates the projection from the first event seen.</summary>
    public static LearnerGroupMemberProjection Create(Guid groupId, Guid ownerId, Guid learnerId, bool isActive, DateTime occurredAtUtc) =>
        new()
        {
            GroupId = groupId,
            OwnerId = ownerId,
            LearnerId = learnerId,
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
