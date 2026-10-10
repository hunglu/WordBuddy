using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>
/// One answered exercise. Insert-only: created through <see cref="Create"/>, no setters and no
/// update methods. Ids, flags and timings only — no answer text (D-5).
/// </summary>
public sealed class ReviewLog : Entity
{
    /// <summary>The learner's user id.</summary>
    public Guid UserId { get; }

    /// <summary>Content's sense id.</summary>
    public Guid SenseId { get; }

    /// <summary>Session id issued by the session endpoint.</summary>
    public Guid SessionId { get; }

    /// <summary>Server time of the answer (UTC).</summary>
    public DateTime OccurredAtUtc { get; }

    /// <summary>Exercise shown.</summary>
    public ExerciseType ExerciseType { get; }

    /// <summary>Skill practised.</summary>
    public VocabularySkill Skill { get; }

    /// <summary>Whether the answer was correct.</summary>
    public bool IsCorrect { get; }

    /// <summary>Response time in milliseconds.</summary>
    public int ResponseMs { get; }

    /// <summary>Whether a hint was used.</summary>
    public bool HintUsed { get; }

    /// <summary>Server-derived: the word was new or due when answered.</summary>
    public bool IsDue { get; }

    /// <summary>Server-derived: 1-based attempt number for this word in this session.</summary>
    public int AttemptNo { get; }

    /// <summary>Server-derived rating.</summary>
    public FsrsRating Rating { get; }

    /// <summary>The <see cref="VocabularyExercise"/> answered; <see langword="null"/> for rows written before WB-28.</summary>
    public Guid? ExerciseId { get; }

    /// <summary>Response time the client reported; <see langword="null"/> for old rows.</summary>
    public int? ClientResponseMs { get; }

    /// <summary>Response time the server measured; <see langword="null"/> for old rows.</summary>
    public int? ServerResponseMs { get; }

    /// <summary><see langword="true"/> when the client time was rejected and the server value was used.</summary>
    public bool TimingAdjusted { get; }

    private ReviewLog(
        Guid id,
        Guid userId,
        Guid senseId,
        Guid sessionId,
        DateTime occurredAtUtc,
        ExerciseType exerciseType,
        VocabularySkill skill,
        bool isCorrect,
        int responseMs,
        bool hintUsed,
        bool isDue,
        int attemptNo,
        FsrsRating rating,
        Guid? exerciseId,
        int? clientResponseMs,
        int? serverResponseMs,
        bool timingAdjusted) : base(id)
    {
        UserId = userId;
        SenseId = senseId;
        SessionId = sessionId;
        OccurredAtUtc = occurredAtUtc;
        ExerciseType = exerciseType;
        Skill = skill;
        IsCorrect = isCorrect;
        ResponseMs = responseMs;
        HintUsed = hintUsed;
        IsDue = isDue;
        AttemptNo = attemptNo;
        Rating = rating;
        ExerciseId = exerciseId;
        ClientResponseMs = clientResponseMs;
        ServerResponseMs = serverResponseMs;
        TimingAdjusted = timingAdjusted;
    }

    /// <summary>Creates a review log row.</summary>
    public static ReviewLog Create(
        Guid id,
        Guid userId,
        Guid senseId,
        Guid sessionId,
        DateTime occurredAtUtc,
        ExerciseType exerciseType,
        VocabularySkill skill,
        bool isCorrect,
        int responseMs,
        bool hintUsed,
        bool isDue,
        int attemptNo,
        FsrsRating rating,
        Guid? exerciseId = null,
        int? clientResponseMs = null,
        int? serverResponseMs = null,
        bool timingAdjusted = false) =>
        new(id, userId, senseId, sessionId, occurredAtUtc, exerciseType, skill, isCorrect, responseMs, hintUsed, isDue, attemptNo, rating,
            exerciseId, clientResponseMs, serverResponseMs, timingAdjusted);
}
