namespace WordBuddy.Progress.Domain;

/// <summary>
/// Picks the exercise for one word. Pure and deterministic. Port of the UI <c>pickExercise</c>.
/// <list type="table">
/// <listheader><term>Level</term><description>Exercise</description></listheader>
/// <item><term>Recognition (New/Learning, no correct answer yet)</term><description>PictureChoice (image) → ListeningChoice (audio) → Typing</description></item>
/// <item><term>Recall (Learning after 1 correct)</term><description>ListeningChoice (audio) → Typing</description></item>
/// <item><term>Review, Mastered, Leech</term><description>Typing</description></item>
/// </list>
/// Choice exercises need at least <see cref="ChoiceOptionCount"/> candidate senses; otherwise Typing.
/// </summary>
public static class ExerciseSelector
{
    /// <summary>A choice exercise shows the right word plus distractors, this many options in total.</summary>
    public const int ChoiceOptionCount = 4;

    /// <summary>The Typing fallback, also used when too few distractors exist.</summary>
    public static ExerciseChoice TypingChoice { get; } = new(ExerciseType.Typing, VocabularySkill.Spelling);

    /// <summary>True for statuses that go straight to recall (Typing).</summary>
    public static bool IsRecallOnly(WordStatus status) =>
        status is WordStatus.Review or WordStatus.Mastered or WordStatus.Leech;

    /// <summary>Level for a word: recall-only statuses and words already answered correctly are at Recall.</summary>
    public static ExerciseLevel LevelFor(WordStatus status, int correctAnswersInSession) =>
        IsRecallOnly(status) || correctAnswersInSession > 0 ? ExerciseLevel.Recall : ExerciseLevel.Recognition;

    /// <summary>Selects the exercise for a word.</summary>
    public static ExerciseChoice Select(WordStatus status, ExerciseLevel level, bool hasImage, bool hasAudio, int candidateSenseCount)
    {
        if (IsRecallOnly(status) || candidateSenseCount < ChoiceOptionCount)
        {
            return TypingChoice;
        }

        if (level == ExerciseLevel.Recognition && hasImage)
        {
            return new ExerciseChoice(ExerciseType.PictureChoice, VocabularySkill.Meaning);
        }

        return hasAudio ? new ExerciseChoice(ExerciseType.ListeningChoice, VocabularySkill.Listening) : TypingChoice;
    }
}
