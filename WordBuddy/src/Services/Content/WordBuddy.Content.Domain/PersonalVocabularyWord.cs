using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A learner-authored vocabulary word in their own personal practice set — deliberately
/// separate from the lesson-scoped, admin-authored <see cref="VocabularyItem"/>. May optionally be
/// submitted for moderation and, once approved, shared into the community pool for other learners
/// to browse and adopt.</summary>
public sealed class PersonalVocabularyWord : Entity
{
    /// <summary>The owning learner's user id — a plain field, not a foreign key, since Content has
    /// no reference to Identity's data (consistent with the independence model).</summary>
    public Guid OwnerUserId { get; }

    /// <summary>The owner's age group, snapshotted at creation time from the caller's JWT claim —
    /// preserves the authoring context for moderation even if the account's age group changes later.</summary>
    public AgeGroup OwnerAgeGroup { get; }

    public string Word { get; }
    public string Definition { get; }
    public string? Example { get; }
    public VocabularyShareStatus ShareStatus { get; private set; }

    /// <summary>Whether a Child-authenticated caller may see this word in the community pool.
    /// Settable only by a moderator, on approval.</summary>
    public bool VisibleToChildren { get; private set; }

    public DateTime CreatedAtUtc { get; }
    public DateTime? ModeratedAtUtc { get; private set; }
    public Guid? ModeratedByUserId { get; private set; }

    public PersonalVocabularyWord(
        Guid id,
        Guid ownerUserId,
        AgeGroup ownerAgeGroup,
        string word,
        string definition,
        string? example)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        OwnerAgeGroup = ownerAgeGroup;
        Word = word;
        Definition = definition;
        Example = example;
        ShareStatus = VocabularyShareStatus.Private;
        VisibleToChildren = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Moves this word from <see cref="VocabularyShareStatus.Private"/> or
    /// <see cref="VocabularyShareStatus.Rejected"/> into <see cref="VocabularyShareStatus.PendingReview"/>.</summary>
    public Result RequestShare()
    {
        if (ShareStatus is not (VocabularyShareStatus.Private or VocabularyShareStatus.Rejected))
        {
            return Result.Failure(Error.Conflict(
                "PersonalVocabularyWord.InvalidShareRequest",
                $"Word {Id} cannot be submitted for review from its current status ({ShareStatus})."));
        }

        ShareStatus = VocabularyShareStatus.PendingReview;
        return Result.Success();
    }

    /// <summary>Approves a <see cref="VocabularyShareStatus.PendingReview"/> word into the community
    /// pool, with the moderator's explicit decision on child visibility.</summary>
    public Result Approve(bool visibleToChildren, Guid moderatorId)
    {
        if (ShareStatus != VocabularyShareStatus.PendingReview)
        {
            return Result.Failure(Error.Conflict(
                "PersonalVocabularyWord.InvalidApproval",
                $"Word {Id} cannot be approved from its current status ({ShareStatus})."));
        }

        ShareStatus = VocabularyShareStatus.Shared;
        VisibleToChildren = visibleToChildren;
        ModeratedAtUtc = DateTime.UtcNow;
        ModeratedByUserId = moderatorId;
        return Result.Success();
    }

    /// <summary>Rejects a <see cref="VocabularyShareStatus.PendingReview"/> word.</summary>
    public Result Reject(Guid moderatorId)
    {
        if (ShareStatus != VocabularyShareStatus.PendingReview)
        {
            return Result.Failure(Error.Conflict(
                "PersonalVocabularyWord.InvalidRejection",
                $"Word {Id} cannot be rejected from its current status ({ShareStatus})."));
        }

        ShareStatus = VocabularyShareStatus.Rejected;
        VisibleToChildren = false;
        ModeratedAtUtc = DateTime.UtcNow;
        ModeratedByUserId = moderatorId;
        return Result.Success();
    }
}
