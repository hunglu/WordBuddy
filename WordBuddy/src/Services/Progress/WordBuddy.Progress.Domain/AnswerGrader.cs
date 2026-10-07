namespace WordBuddy.Progress.Domain;

/// <summary>
/// Maps an answer to an FSRS rating. Pure. Wrong → Again; correct with hint or slow → Hard;
/// correct and fast → Easy; else Good. Thresholds per <see cref="ExerciseType"/>, scaled by the
/// <see cref="AgeGroup"/> multiplier (D-7).
/// </summary>
public sealed class AnswerGrader
{
    private readonly VocabularyGradingOptions _options;

    /// <summary>Creates a grader with the configured thresholds.</summary>
    public AnswerGrader(VocabularyGradingOptions options)
    {
        _options = options;
    }

    /// <summary>Derives the rating for one answer.</summary>
    public FsrsRating Grade(ExerciseType exerciseType, AgeGroup ageGroup, bool isCorrect, int responseMs, bool hintUsed)
    {
        if (!isCorrect)
        {
            return FsrsRating.Again;
        }

        ResponseTimeThresholds thresholds = _options.For(exerciseType);
        double multiplier = _options.For(ageGroup);
        double slowMs = thresholds.SlowMs * multiplier;
        double fastMs = thresholds.FastMs * multiplier;

        if (hintUsed || responseMs >= slowMs)
        {
            return FsrsRating.Hard;
        }

        return responseMs <= fastMs ? FsrsRating.Easy : FsrsRating.Good;
    }
}
