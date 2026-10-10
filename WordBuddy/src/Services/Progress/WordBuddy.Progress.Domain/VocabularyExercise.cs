using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>
/// One exercise issued to a learner. Progress builds it, stores the expected answer and grades the
/// learner's raw answer; the client never sees the answer. Answered at most once. No learner
/// free text is stored.
/// </summary>
public sealed class VocabularyExercise : Entity
{
    /// <summary>The exercise id (same as <see cref="Entity.Id"/>).</summary>
    public Guid ExerciseId => Id;

    /// <summary>The learner's user id.</summary>
    public Guid UserId { get; }

    /// <summary>Session the exercise belongs to.</summary>
    public Guid SessionId { get; }

    /// <summary>Content's sense id being practised.</summary>
    public Guid SenseId { get; }

    /// <summary>Exercise shown.</summary>
    public ExerciseType ExerciseType { get; }

    /// <summary>Skill practised.</summary>
    public VocabularySkill Skill { get; }

    /// <summary>Normalised word (Typing) or the correct option key (choice).</summary>
    public string ExpectedAnswer { get; }

    /// <summary>The word as written, shown to the learner after the answer.</summary>
    public string CorrectWord { get; }

    /// <summary>Option key → sense id. Empty for Typing.</summary>
    public IReadOnlyDictionary<string, Guid> Options { get; }

    /// <summary>Server time the exercise was issued (UTC).</summary>
    public DateTime IssuedAtUtc { get; }

    /// <summary>Server time of the answer (UTC), or <see langword="null"/> while open.</summary>
    public DateTime? AnsweredAtUtc { get; private set; }

    private VocabularyExercise(
        Guid id,
        Guid userId,
        Guid sessionId,
        Guid senseId,
        ExerciseType exerciseType,
        VocabularySkill skill,
        string expectedAnswer,
        string correctWord,
        IReadOnlyDictionary<string, Guid> options,
        DateTime issuedAtUtc) : base(id)
    {
        UserId = userId;
        SessionId = sessionId;
        SenseId = senseId;
        ExerciseType = exerciseType;
        Skill = skill;
        ExpectedAnswer = expectedAnswer;
        CorrectWord = correctWord;
        Options = options;
        IssuedAtUtc = issuedAtUtc;
    }

    /// <summary>Creates an open exercise.</summary>
    public static VocabularyExercise Create(
        Guid id,
        Guid userId,
        Guid sessionId,
        Guid senseId,
        ExerciseType exerciseType,
        VocabularySkill skill,
        string expectedAnswer,
        string correctWord,
        IReadOnlyDictionary<string, Guid> options,
        DateTime issuedAtUtc) =>
        new(id, userId, sessionId, senseId, exerciseType, skill, expectedAnswer, correctWord, options, issuedAtUtc);

    /// <summary>Marks the exercise answered. A second call fails with <c>Exercise.AlreadyAnswered</c>.</summary>
    public Result Answer(DateTime nowUtc)
    {
        if (AnsweredAtUtc is not null)
        {
            return Result.Failure(Error.Conflict("Exercise.AlreadyAnswered", "This exercise was already answered."));
        }

        AnsweredAtUtc = nowUtc;
        return Result.Success();
    }
}
