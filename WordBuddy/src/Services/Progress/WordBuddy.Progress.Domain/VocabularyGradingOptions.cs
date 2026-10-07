namespace WordBuddy.Progress.Domain;

/// <summary>Grading options, bound from <c>Vocabulary:Grading</c> (D-7).</summary>
public sealed class VocabularyGradingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Vocabulary:Grading";

    /// <summary>Thresholds for <see cref="ExerciseType.PictureChoice"/>. Default 3 s / 10 s.</summary>
    public ResponseTimeThresholds PictureChoice { get; set; } = new() { FastMs = 3000, SlowMs = 10000 };

    /// <summary>Thresholds for <see cref="ExerciseType.ListeningChoice"/>. Default 4 s / 12 s.</summary>
    public ResponseTimeThresholds ListeningChoice { get; set; } = new() { FastMs = 4000, SlowMs = 12000 };

    /// <summary>Thresholds for <see cref="ExerciseType.Typing"/>. Default 6 s / 20 s.</summary>
    public ResponseTimeThresholds Typing { get; set; } = new() { FastMs = 6000, SlowMs = 20000 };

    /// <summary>Multiplier applied to every threshold per <see cref="AgeGroup"/>.</summary>
    public AgeGroupMultipliers AgeGroupMultiplier { get; set; } = new();

    /// <summary>Returns the base thresholds for <paramref name="exerciseType"/>.</summary>
    public ResponseTimeThresholds For(ExerciseType exerciseType) => exerciseType switch
    {
        ExerciseType.PictureChoice => PictureChoice,
        ExerciseType.ListeningChoice => ListeningChoice,
        ExerciseType.Typing => Typing,
        _ => throw new ArgumentOutOfRangeException(nameof(exerciseType), exerciseType, "Unknown exercise type."),
    };

    /// <summary>Returns the multiplier for <paramref name="ageGroup"/>.</summary>
    public double For(AgeGroup ageGroup) => ageGroup == AgeGroup.Child ? AgeGroupMultiplier.Child : AgeGroupMultiplier.Adult;
}

/// <summary>Fast and slow response-time thresholds in milliseconds.</summary>
public sealed class ResponseTimeThresholds
{
    /// <summary>At or below this, a correct answer is <see cref="FsrsRating.Easy"/>.</summary>
    public int FastMs { get; set; }

    /// <summary>At or above this, a correct answer is <see cref="FsrsRating.Hard"/>.</summary>
    public int SlowMs { get; set; }
}

/// <summary>Threshold multipliers per <see cref="AgeGroup"/>. Default Child 1.25, Adult 1.0.</summary>
public sealed class AgeGroupMultipliers
{
    /// <summary>Multiplier for child accounts.</summary>
    public double Child { get; set; } = 1.25;

    /// <summary>Multiplier for adult accounts.</summary>
    public double Adult { get; set; } = 1.0;
}
