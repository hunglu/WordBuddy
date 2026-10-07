using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>
/// FSRS learning state of one sense for one learner — one row per (user, sense). Follows the
/// <see cref="LearnerWordMembership"/>: removing a word deactivates the state, re-adding it
/// re-activates it with its FSRS history intact. Ids only, no word text.
/// </summary>
public sealed class LearnerWordState : Entity
{
    /// <summary>The learner's user id (plain field, not a foreign key).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Content's sense id (plain field, not a foreign key).</summary>
    public Guid SenseId { get; private set; }

    /// <summary>Learner-facing status derived from the FSRS state.</summary>
    public WordStatus Status { get; private set; }

    /// <summary>FSRS stability in days (0 before the first review).</summary>
    public double Stability { get; private set; }

    /// <summary>FSRS difficulty 1–10 (0 before the first review).</summary>
    public double Difficulty { get; private set; }

    /// <summary>Next due time (UTC). For a new word: when it was added.</summary>
    public DateTime DueAtUtc { get; private set; }

    /// <summary>Number of reviews that changed the schedule.</summary>
    public int Reps { get; private set; }

    /// <summary>Number of times a word in <see cref="FsrsPhase.Review"/> was forgotten.</summary>
    public int Lapses { get; private set; }

    /// <summary>FSRS card state.</summary>
    public FsrsPhase FsrsPhase { get; private set; }

    /// <summary>FSRS (re)learning step; <see langword="null"/> in <see cref="Domain.FsrsPhase.Review"/>.</summary>
    public int? FsrsStep { get; private set; }

    /// <summary>Last scheduling review (UTC).</summary>
    public DateTime? LastReviewedAtUtc { get; private set; }

    /// <summary>First scheduling review (UTC) — counts the word as "introduced" that day.</summary>
    public DateTime? FirstReviewedAtUtc { get; private set; }

    /// <summary><see langword="true"/> while the sense is in the learner's list.</summary>
    public bool IsActive { get; private set; }

    private LearnerWordState(Guid id, Guid userId, Guid senseId) : base(id)
    {
        UserId = userId;
        SenseId = senseId;
    }

    /// <summary>Creates an active, never-reviewed state due at <paramref name="addedAtUtc"/>.</summary>
    public static LearnerWordState CreateNew(Guid id, Guid userId, Guid senseId, DateTime addedAtUtc)
    {
        FsrsCard card = FsrsCard.New(addedAtUtc);
        return new LearnerWordState(id, userId, senseId)
        {
            Status = WordStatus.New,
            Stability = card.Stability,
            Difficulty = card.Difficulty,
            DueAtUtc = card.DueAtUtc,
            FsrsPhase = card.Phase,
            FsrsStep = card.Step,
            Reps = 0,
            Lapses = 0,
            IsActive = true,
        };
    }

    /// <summary>Applies one scheduling review: updates FSRS fields, counters and <see cref="Status"/>.</summary>
    public void ApplyReview(FsrsRating rating, DateTime nowUtc, IFsrsScheduler scheduler, VocabularySchedulingOptions options)
    {
        FsrsCard current = new(FsrsPhase, FsrsStep, Stability, Difficulty, DueAtUtc, LastReviewedAtUtc);
        FsrsCard next = scheduler.Schedule(current, rating, nowUtc);

        if (FsrsPhase == FsrsPhase.Review && rating == FsrsRating.Again)
        {
            Lapses++;
        }

        Reps++;
        FsrsPhase = next.Phase;
        FsrsStep = next.Step;
        Stability = next.Stability;
        Difficulty = next.Difficulty;
        DueAtUtc = next.DueAtUtc;
        LastReviewedAtUtc = nowUtc;
        FirstReviewedAtUtc ??= nowUtc;
        Status = DeriveStatus(options);
    }

    /// <summary>Marks the state active again (word re-added). FSRS data is kept.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Marks the state inactive (word removed). FSRS data is kept.</summary>
    public void Deactivate() => IsActive = false;

    private WordStatus DeriveStatus(VocabularySchedulingOptions options)
    {
        if (Reps == 0)
        {
            return WordStatus.New;
        }

        if (Lapses >= options.LeechLapses)
        {
            return WordStatus.Leech;
        }

        if (Stability >= options.MasteredStabilityDays)
        {
            return WordStatus.Mastered;
        }

        return FsrsPhase == FsrsPhase.Review ? WordStatus.Review : WordStatus.Learning;
    }
}
