using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>
/// Progress's copy of "this learner has this sense in their list", built from Content's
/// <c>LearnerWordAdded</c> / <c>LearnerWordRemoved</c> events. Ids and timestamps only — no word
/// text. A removal keeps the row (<see cref="IsActive"/> = <see langword="false"/>) so a late,
/// older <c>Added</c> cannot bring it back. Events older than <see cref="LastEventAtUtc"/> are ignored.
/// </summary>
public sealed class LearnerWordMembership : Entity
{
    /// <summary>The learner's user id (plain field, not a foreign key).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Content's sense id (plain field, not a foreign key).</summary>
    public Guid SenseId { get; private set; }

    /// <summary>User who caused the latest add. <see cref="Guid.Empty"/> when only a removal was seen.</summary>
    public Guid AddedBy { get; private set; }

    /// <summary>When the latest add happened in Content (UTC).</summary>
    public DateTime AddedAtUtc { get; private set; }

    /// <summary><see langword="true"/> while the sense is in the learner's list.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Timestamp of the newest applied event (UTC). Older events are ignored.</summary>
    public DateTime LastEventAtUtc { get; private set; }

    private LearnerWordMembership(Guid id, Guid userId, Guid senseId) : base(id)
    {
        UserId = userId;
        SenseId = senseId;
    }

    /// <summary>Creates an active membership from an add event.</summary>
    public static LearnerWordMembership CreateAdded(Guid id, Guid userId, Guid senseId, Guid addedBy, DateTime addedAtUtc)
    {
        LearnerWordMembership membership = new(id, userId, senseId);
        membership.Activate(addedBy, addedAtUtc);
        return membership;
    }

    /// <summary>Creates an inactive membership from a remove event that arrived first, so an older
    /// add arriving later stays ignored.</summary>
    public static LearnerWordMembership CreateRemoved(Guid id, Guid userId, Guid senseId, DateTime removedAtUtc)
    {
        LearnerWordMembership membership = new(id, userId, senseId)
        {
            AddedBy = Guid.Empty,
            AddedAtUtc = removedAtUtc,
            IsActive = false,
            LastEventAtUtc = removedAtUtc,
        };
        return membership;
    }

    /// <summary>Applies an add event. Returns <see langword="true"/> when applied,
    /// <see langword="false"/> when ignored because it is older than <see cref="LastEventAtUtc"/>.</summary>
    public Result<bool> RecordAdded(Guid addedBy, DateTime addedAtUtc)
    {
        if (addedAtUtc < LastEventAtUtc)
        {
            return Result.Success(false);
        }

        Activate(addedBy, addedAtUtc);
        return Result.Success(true);
    }

    /// <summary>Applies a remove event. Returns <see langword="true"/> when applied,
    /// <see langword="false"/> when ignored because it is older than <see cref="LastEventAtUtc"/>.</summary>
    public Result<bool> RecordRemoved(DateTime removedAtUtc)
    {
        if (removedAtUtc < LastEventAtUtc)
        {
            return Result.Success(false);
        }

        IsActive = false;
        LastEventAtUtc = removedAtUtc;
        return Result.Success(true);
    }

    private void Activate(Guid addedBy, DateTime addedAtUtc)
    {
        AddedBy = addedBy;
        AddedAtUtc = addedAtUtc;
        IsActive = true;
        LastEventAtUtc = addedAtUtc;
    }
}
