using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>
/// One-way link: an adult supporter supports a learner. Every active supporter has the same fixed
/// permission set; <see cref="Relationship"/> is a display label only. Create through
/// <see cref="SupportLinkPolicy"/>, which applies the child/adult rules.
/// </summary>
public sealed class SupportLink : Entity
{
    /// <summary>Gets the supported learner.</summary>
    public Guid LearnerId { get; private set; }

    /// <summary>Gets the supporter. Always an adult account.</summary>
    public Guid SupporterId { get; private set; }

    /// <summary>Gets a value indicating whether this is the child learner's Primary link. Always false for adult learners.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets the optional display label.</summary>
    public SupportRelationship? Relationship { get; private set; }

    /// <summary>Gets the current status.</summary>
    public SupportLinkStatus Status { get; private set; }

    /// <summary>Gets the user who granted the link (the accepting side).</summary>
    public Guid GrantedBy { get; private set; }

    /// <summary>Gets when the link was created, UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Gets when the link last changed, UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private SupportLink(Guid id) : base(id)
    {
    }

    internal static SupportLink Create(
        Guid id,
        Guid learnerId,
        Guid supporterId,
        bool isPrimary,
        SupportRelationship? relationship,
        SupportLinkStatus status,
        Guid grantedBy,
        DateTime nowUtc) =>
        new(id)
        {
            LearnerId = learnerId,
            SupporterId = supporterId,
            IsPrimary = isPrimary,
            Relationship = relationship,
            Status = status,
            GrantedBy = grantedBy,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };

    /// <summary>Gets a value indicating whether the link is active.</summary>
    public bool IsActive => Status == SupportLinkStatus.Active;

    /// <summary>Primary approves a pending extra supporter.</summary>
    public Result ApproveByPrimary(DateTime nowUtc)
    {
        if (Status != SupportLinkStatus.PendingPrimaryApproval)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        Status = SupportLinkStatus.Active;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>Primary rejects a pending extra supporter.</summary>
    public Result RejectByPrimary(DateTime nowUtc)
    {
        if (Status != SupportLinkStatus.PendingPrimaryApproval)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        Status = SupportLinkStatus.Revoked;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>Ends an active link. A Primary link cannot be revoked; hand Primary over first.</summary>
    public Result Revoke(DateTime nowUtc)
    {
        if (Status != SupportLinkStatus.Active)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        if (IsPrimary)
        {
            return Result.Failure(SupportLinkErrors.PrimaryCannotBeUnlinked);
        }

        Status = SupportLinkStatus.Revoked;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>Makes this active link the Primary one (admin handover).</summary>
    public Result MakePrimary(DateTime nowUtc)
    {
        if (Status != SupportLinkStatus.Active)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        IsPrimary = true;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>Removes the Primary flag (the other half of an admin handover).</summary>
    public void ClearPrimary(DateTime nowUtc)
    {
        IsPrimary = false;
        UpdatedAtUtc = nowUtc;
    }
}
