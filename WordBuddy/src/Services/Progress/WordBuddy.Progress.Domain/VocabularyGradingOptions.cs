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

    /// <summary>
    /// Allowed gap in milliseconds between the server-measured time and the client-reported time.
    /// Covers network and render delay. Default 3000.
    /// </summary>
    public int ToleranceMs { get; set; } = 3000;

    /// <summary>Multiplier applied to every threshold per <see cref="AgeGroup"/>.</summary>
    public AgeGroupMultipliers AgeGroupMultiplier { get; set; } = new();

    /// <summary>Multiplier applied to every threshold per <see cref="AgeBand"/>. Wins over <see cref="AgeGroupMultiplier"/> when the band is known.</summary>
    public AgeBandMultipliers AgeBandMultiplier { get; set; } = new();

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

    /// <summary>Returns the multiplier for <paramref name="ageBand"/>.</summary>
    public double For(AgeBand ageBand) => ageBand switch
    {
        AgeBand.Under7 => AgeBandMultiplier.Under7,
        AgeBand.Age7To9 => AgeBandMultiplier.Age7To9,
        AgeBand.Age10To12 => AgeBandMultiplier.Age10To12,
        AgeBand.Age13To17 => AgeBandMultiplier.Age13To17,
        AgeBand.Adult => AgeBandMultiplier.Adult,
        _ => throw new ArgumentOutOfRangeException(nameof(ageBand), ageBand, "Unknown age band."),
    };
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

/// <summary>Threshold multipliers per <see cref="AgeBand"/>. Defaults 1.5, 1.35, 1.2, 1.1, 1.0.</summary>
public sealed class AgeBandMultipliers
{
    /// <summary>Multiplier for children under 7.</summary>
    public double Under7 { get; set; } = 1.5;

    /// <summary>Multiplier for ages 7 to 9.</summary>
    public double Age7To9 { get; set; } = 1.35;

    /// <summary>Multiplier for ages 10 to 12.</summary>
    public double Age10To12 { get; set; } = 1.2;

    /// <summary>Multiplier for ages 13 to 17.</summary>
    public double Age13To17 { get; set; } = 1.1;

    /// <summary>Multiplier for adults (18 and older).</summary>
    public double Adult { get; set; } = 1.0;
}
