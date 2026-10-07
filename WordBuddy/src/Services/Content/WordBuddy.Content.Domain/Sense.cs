using System.Security.Cryptography;
using System.Text;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>One meaning of a <see cref="Lexeme"/> (formerly <c>VocabularyWord</c>), whether an admin
/// wrote it for a lesson (<see cref="VocabularySource.System"/>) or a learner added it
/// (<see cref="VocabularySource.Learner"/>). Lessons reach their senses through
/// <see cref="LessonSense"/>; learners reach theirs through <see cref="LearnerWord"/>.
/// Learner words may be submitted for moderation and, once approved, shared into the community pool.</summary>
public sealed class Sense : Entity
{
    /// <summary>Separator between the normalized parts of <see cref="ContentHash"/>. Must match the
    /// <c>NCHAR(31)</c> used by the <c>UnifyVocabularyWords</c> migration's SQL.</summary>
    private const char HashSeparator = '\u001f';

    public string Word { get; private set; }
    public string Definition { get; private set; }
    public string? Example { get; private set; }

    /// <summary>The <see cref="Lexeme"/> this sense belongs to. Set by id only: the repository
    /// finds or creates the lexeme first.</summary>
    public Guid LexemeId { get; private set; }

    /// <summary>Upper-case hex SHA-256 of the normalized Word, Definition and Example. See
    /// <see cref="ComputeContentHash"/>.</summary>
    public string ContentHash { get; private set; }

    public Guid? AudioAssetId { get; private set; }
    public MediaAsset? Audio { get; private set; }

    /// <summary>Optional picture for the sense, used by the picture-choice review exercise.</summary>
    public Guid? ImageAssetId { get; private set; }

    /// <summary>Navigation to the picture asset, when loaded.</summary>
    public MediaAsset? Image { get; private set; }

    public VocabularySource Source { get; private set; }

    /// <summary>The author — a learner's user id, or <see cref="SystemOwner.UserId"/> for system
    /// words. A plain field, not a foreign key: Content never references Identity's data.</summary>
    public Guid OwnerUserId { get; private set; }

    /// <summary>The author's age group, snapshotted at creation from the JWT claim, kept for
    /// moderation context. <see langword="null"/> for system words.</summary>
    public AgeGroup? OwnerAgeGroup { get; private set; }

    public VocabularyShareStatus ShareStatus { get; private set; }

    /// <summary>Whether a Child caller may see this word in the community pool. Set only by a
    /// moderator, on approval.</summary>
    public bool VisibleToChildren { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModeratedAtUtc { get; private set; }
    public Guid? ModeratedByUserId { get; private set; }

    private Sense(
        Guid id,
        string word,
        string definition,
        string? example,
        Guid lexemeId,
        string contentHash,
        Guid? audioAssetId,
        VocabularySource source,
        Guid ownerUserId,
        AgeGroup? ownerAgeGroup,
        VocabularyShareStatus shareStatus,
        bool visibleToChildren,
        DateTime createdAtUtc)
        : base(id)
    {
        Word = word;
        Definition = definition;
        Example = example;
        LexemeId = lexemeId;
        ContentHash = contentHash;
        AudioAssetId = audioAssetId;
        Source = source;
        OwnerUserId = ownerUserId;
        OwnerAgeGroup = ownerAgeGroup;
        ShareStatus = shareStatus;
        VisibleToChildren = visibleToChildren;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates an admin-authored lesson word owned by <see cref="SystemOwner.UserId"/>.
    /// System words are always <see cref="VocabularyShareStatus.Private"/>, so they never enter
    /// the shared pool.</summary>
    public static Sense CreateSystem(Guid id, Guid lexemeId, string word, string definition, string example, Guid? audioAssetId = null) =>
        new(
            id,
            word,
            definition,
            example,
            lexemeId,
            ComputeContentHash(word, definition, example),
            audioAssetId,
            VocabularySource.System,
            SystemOwner.UserId,
            ownerAgeGroup: null,
            VocabularyShareStatus.Private,
            visibleToChildren: false,
            DateTime.UtcNow);

    /// <summary>Creates a learner-authored word. Fails if <paramref name="ownerUserId"/> is empty
    /// or is <see cref="SystemOwner.UserId"/>.</summary>
    public static Result<Sense> CreateLearner(
        Guid id,
        Guid lexemeId,
        Guid ownerUserId,
        AgeGroup ownerAgeGroup,
        string word,
        string definition,
        string? example)
    {
        Result ownerCheck = ValidateLearnerOwner(ownerUserId);
        if (ownerCheck.IsFailure)
        {
            return Result.Failure<Sense>(ownerCheck.Error);
        }

        return Result.Success(new Sense(
            id,
            word,
            definition,
            example,
            lexemeId,
            ComputeContentHash(word, definition, example),
            audioAssetId: null,
            VocabularySource.Learner,
            ownerUserId,
            ownerAgeGroup,
            VocabularyShareStatus.Private,
            visibleToChildren: false,
            DateTime.UtcNow));
    }

    /// <summary>Checks that a learner sense has a real learner owner (not empty, not the system
    /// owner). <see cref="CreateLearner"/> runs the same check; callers run it first so no lexeme
    /// is created for a sense that would be rejected.</summary>
    public static Result ValidateLearnerOwner(Guid ownerUserId) =>
        ownerUserId == Guid.Empty || ownerUserId == SystemOwner.UserId
            ? Result.Failure(Error.Validation(
                "PersonalVocabularyWord.InvalidOwner",
                "A learner word must be owned by a learner, not the system owner."))
            : Result.Success();

    /// <summary>Normalizes a word for lookup: trims spaces and upper-cases it. Mirrors SQL's
    /// <c>UPPER(LTRIM(RTRIM(...)))</c>, which trims spaces only.</summary>
    public static string NormalizeWord(string word) => word.Trim(' ').ToUpperInvariant();

    /// <summary>Computes the dedupe key: upper-case hex SHA-256 over the UTF-16LE bytes of the
    /// normalized Word, Definition and Example (null Example = empty), joined by U+001F. Mirrors
    /// <c>CONVERT(char(64), HASHBYTES('SHA2_256', ...), 2)</c> over <c>NVARCHAR</c> in SQL Server.
    /// <para><b>Accepted limitation:</b> <see cref="string.ToUpperInvariant"/> and SQL Server's
    /// collation-dependent <c>UPPER</c> can disagree for a few rare letters (for example <c>ß</c>,
    /// ligatures). A word migrated by <c>UnifyVocabularyWords</c> and re-added later may then hash
    /// differently and get a second row instead of a link — a harmless duplicate that breaks no
    /// constraint and leaks nothing. Not corrected by design.</para></summary>
    public static string ComputeContentHash(string word, string definition, string? example)
    {
        string normalized = string.Concat(
            NormalizeWord(word), HashSeparator,
            NormalizeWord(definition), HashSeparator,
            NormalizeWord(example ?? string.Empty));

        byte[] hash = SHA256.HashData(Encoding.Unicode.GetBytes(normalized));
        return Convert.ToHexString(hash);
    }

    /// <summary>Moves a learner word from <see cref="VocabularyShareStatus.Private"/> or
    /// <see cref="VocabularyShareStatus.Rejected"/> into <see cref="VocabularyShareStatus.PendingReview"/>.
    /// System words can never be shared.</summary>
    public Result RequestShare()
    {
        if (Source == VocabularySource.System ||
            ShareStatus is not (VocabularyShareStatus.Private or VocabularyShareStatus.Rejected))
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

    /// <summary>Hands a <see cref="VocabularyShareStatus.Shared"/> learner word over to the system
    /// owner when its author deletes it. The word stays in the community pool: share status,
    /// <see cref="VisibleToChildren"/> and the moderation fields are kept.</summary>
    public Result TransferToSystem()
    {
        if (Source != VocabularySource.Learner || ShareStatus != VocabularyShareStatus.Shared)
        {
            return Result.Failure(Error.Conflict(
                "PersonalVocabularyWord.InvalidTransfer",
                $"Word {Id} cannot be transferred to the system from its current status ({ShareStatus})."));
        }

        Source = VocabularySource.System;
        OwnerUserId = SystemOwner.UserId;
        OwnerAgeGroup = null;
        return Result.Success();
    }

    /// <summary>Cancels a share request: moves a <see cref="VocabularyShareStatus.PendingReview"/>
    /// word back to <see cref="VocabularyShareStatus.Private"/>.</summary>
    public Result CancelShareRequest()
    {
        if (ShareStatus != VocabularyShareStatus.PendingReview)
        {
            return Result.Failure(Error.Conflict(
                "PersonalVocabularyWord.InvalidShareCancellation",
                $"Word {Id} has no share request to cancel ({ShareStatus})."));
        }

        ShareStatus = VocabularyShareStatus.Private;
        return Result.Success();
    }

    /// <summary>Attaches a picture (a <see cref="MediaAsset"/> id) to this sense, replacing any
    /// previous one.</summary>
    public void AttachImage(Guid imageAssetId)
    {
        ImageAssetId = imageAssetId;
    }

    /// <summary>Whether a caller may see (and therefore be linked to) this word: they wrote it, it
    /// is a non-shared system (lesson) word, or it is <see cref="VocabularyShareStatus.Shared"/> —
    /// and, for a Child caller, cleared as <see cref="VisibleToChildren"/>. A shared word always
    /// applies the child filter, also after it was transferred to the system owner.</summary>
    public bool IsVisibleTo(Guid userId, AgeGroup ageGroup) =>
        (ShareStatus == VocabularyShareStatus.Shared && (ageGroup == AgeGroup.Adult || VisibleToChildren)) ||
        (ShareStatus != VocabularyShareStatus.Shared && Source == VocabularySource.System) ||
        (Source == VocabularySource.Learner && OwnerUserId == userId);
}
