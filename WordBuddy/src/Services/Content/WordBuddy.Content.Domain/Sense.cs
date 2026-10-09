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

    /// <summary>Maximum number of <see cref="Examples"/>.</summary>
    public const int MaxExamples = 3;

    /// <summary>Maximum length of <see cref="RegisterNote"/>.</summary>
    public const int RegisterNoteMaxLength = 200;

    /// <summary>Maximum number of items in each enrichment list.</summary>
    public const int MaxEnrichmentItems = 10;

    /// <summary>Maximum length of one example or enrichment item.</summary>
    public const int EnrichmentItemMaxLength = 500;

    private readonly List<SenseTranslation> _translations = [];

    /// <summary>How the content was produced. Existing rows are <see cref="SenseOrigin.Manual"/>.</summary>
    public SenseOrigin Origin { get; private set; }

    /// <summary>Up to <see cref="MaxExamples"/> example sentences (JSON list). Never null.
    /// <see cref="Example"/> stays the first one, so the dedupe hash is unchanged.</summary>
    public IReadOnlyList<string> Examples { get; private set; } = [];

    /// <summary>Common word combinations (JSON list). Never null.</summary>
    public IReadOnlyList<string> Collocations { get; private set; } = [];

    /// <summary>Synonyms (JSON list). Never null.</summary>
    public IReadOnlyList<string> Synonyms { get; private set; } = [];

    /// <summary>Antonyms (JSON list). Never null.</summary>
    public IReadOnlyList<string> Antonyms { get; private set; } = [];

    /// <summary>Topic tags such as <c>food</c> (JSON list). Never null.</summary>
    public IReadOnlyList<string> TopicTags { get; private set; } = [];

    /// <summary>Optional usage register note (formal, slang, ...), at most <see cref="RegisterNoteMaxLength"/>.</summary>
    public string? RegisterNote { get; private set; }

    /// <summary>Auto-fill only: the generator's hint whether the sense suits children. Shown to
    /// approvers only; approval is still required.</summary>
    public bool? ChildSuitableHint { get; private set; }

    /// <summary>Navigation to the lexeme, when loaded.</summary>
    public Lexeme? Lexeme { get; private set; }

    /// <summary>Translations of this sense, when loaded.</summary>
    public IReadOnlyList<SenseTranslation> Translations => _translations;

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

    /// <summary>Creates an auto-filled catalog sense: <see cref="VocabularySource.System"/>,
    /// <see cref="VocabularyShareStatus.Shared"/>, <see cref="SenseOrigin.AutoFill"/>, and not
    /// <see cref="VisibleToChildren"/> until an admin approves it. <see cref="Example"/> is the first
    /// example. Fails on an empty definition, too many examples, or a too long note.</summary>
    public static Result<Sense> CreateAutoFill(
        Guid id,
        Guid lexemeId,
        string word,
        string definition,
        IReadOnlyList<string>? examples,
        IReadOnlyList<string>? collocations = null,
        IReadOnlyList<string>? synonyms = null,
        IReadOnlyList<string>? antonyms = null,
        IReadOnlyList<string>? topicTags = null,
        string? registerNote = null,
        bool? childSuitableHint = null)
    {
        string trimmedWord = (word ?? string.Empty).Trim();
        string trimmedDefinition = (definition ?? string.Empty).Trim();
        if (trimmedWord.Length == 0 || trimmedDefinition.Length == 0)
        {
            return Result.Failure<Sense>(Error.Validation(
                "Sense.EmptyAutoFill", "An auto-filled sense needs a word and a definition."));
        }

        List<string> cleanExamples = CleanList(examples);
        if (cleanExamples.Count > MaxExamples)
        {
            return Result.Failure<Sense>(Error.Validation(
                "Sense.TooManyExamples", $"A sense may have at most {MaxExamples} examples."));
        }

        string? note = string.IsNullOrWhiteSpace(registerNote) ? null : registerNote.Trim();
        if (note is { Length: > RegisterNoteMaxLength })
        {
            return Result.Failure<Sense>(Error.Validation(
                "Sense.RegisterNoteTooLong", $"A register note may have at most {RegisterNoteMaxLength} characters."));
        }

        string? firstExample = cleanExamples.Count > 0 ? cleanExamples[0] : null;
        Sense sense = new(
            id,
            trimmedWord,
            trimmedDefinition,
            firstExample,
            lexemeId,
            ComputeContentHash(trimmedWord, trimmedDefinition, firstExample),
            audioAssetId: null,
            VocabularySource.System,
            SystemOwner.UserId,
            ownerAgeGroup: null,
            VocabularyShareStatus.Shared,
            visibleToChildren: false,
            DateTime.UtcNow)
        {
            Origin = SenseOrigin.AutoFill,
            Examples = cleanExamples,
            Collocations = CleanList(collocations),
            Synonyms = CleanList(synonyms),
            Antonyms = CleanList(antonyms),
            TopicTags = CleanList(topicTags),
            RegisterNote = note,
            ChildSuitableHint = childSuitableHint,
        };

        return Result.Success(sense);
    }

    /// <summary>Trims items, drops empty and duplicate ones, caps length and count. Never null.</summary>
    private static List<string> CleanList(IReadOnlyList<string>? items) =>
        (items ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .Select(i => i.Length > EnrichmentItemMaxLength ? i[..EnrichmentItemMaxLength] : i)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxEnrichmentItems)
            .ToList();

    /// <summary>Admin approval of an auto-filled sense for every child (global).</summary>
    public Result ApproveForChildren(Guid adminId)
    {
        if (Origin != SenseOrigin.AutoFill || VisibleToChildren)
        {
            return Result.Failure(Error.Conflict(
                "Sense.InvalidChildApproval",
                $"Sense {Id} is not an auto-filled sense waiting for child approval."));
        }

        VisibleToChildren = true;
        ModeratedAtUtc = DateTime.UtcNow;
        ModeratedByUserId = adminId;
        return Result.Success();
    }

    /// <summary>Moves a catalog sense (<see cref="VocabularySource.System"/> or
    /// <see cref="VocabularyShareStatus.Shared"/>) to another lexeme, when auto-fill learns its part
    /// of speech. Private learner senses are never moved.</summary>
    public Result MoveToLexeme(Guid lexemeId)
    {
        if (Source != VocabularySource.System && ShareStatus != VocabularyShareStatus.Shared)
        {
            return Result.Failure(Error.Conflict(
                "Sense.InvalidLexemeMove", $"Sense {Id} is private and cannot be moved."));
        }

        LexemeId = lexemeId;
        return Result.Success();
    }

    /// <summary>Adds a translation of this sense. Replaces nothing: one translation per locale.</summary>
    public Result AddTranslation(SenseTranslation translation)
    {
        if (translation.SenseId != Id || _translations.Any(t => t.Locale == translation.Locale))
        {
            return Result.Failure(Error.Conflict(
                "Sense.InvalidTranslation", $"Sense {Id} already has a '{translation.Locale}' translation."));
        }

        _translations.Add(translation);
        return Result.Success();
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

    /// <summary>Like <see cref="IsVisibleTo(Guid, AgeGroup)"/>, plus the caller's own link: a Child
    /// also sees an <see cref="SenseOrigin.AutoFill"/> sense whose link a supporter approved. The
    /// adult rule is unchanged.</summary>
    public bool IsVisibleTo(Guid userId, AgeGroup ageGroup, LearnerWord? callerLink) =>
        IsVisibleTo(userId, ageGroup) ||
        (ageGroup == AgeGroup.Child &&
         Origin == SenseOrigin.AutoFill &&
         callerLink is { ChildApprovedAtUtc: not null } link &&
         link.UserId == userId &&
         link.SenseId == Id);

    /// <summary>Whether a Child caller must wait for approval before seeing this sense's content
    /// through their own link: an unapproved <see cref="SenseOrigin.AutoFill"/> sense. Always
    /// <see langword="false"/> for adults.</summary>
    public bool IsAwaitingChildApproval(AgeGroup ageGroup, LearnerWord? callerLink) =>
        ageGroup == AgeGroup.Child &&
        Origin == SenseOrigin.AutoFill &&
        !VisibleToChildren &&
        callerLink?.ChildApprovedAtUtc is null;
}
