namespace WordBuddy.Progress.Domain;

/// <summary>
/// Own port of the FSRS-6 scheduler (py-fsrs 6.x, MIT) with default parameters. Deterministic:
/// no interval fuzz and no parameter optimiser (decision D-1).
/// </summary>
public sealed class FsrsScheduler : IFsrsScheduler
{
    /// <summary>FSRS-6 default parameters (w0–w20).</summary>
    public static readonly IReadOnlyList<double> DefaultParameters =
    [
        0.212, 1.2931, 2.3065, 8.2956, 6.4133, 0.8334, 3.0194, 0.001, 1.8722, 0.1666, 0.796,
        1.4835, 0.0614, 0.2629, 1.6483, 0.6014, 1.8729, 0.5425, 0.0912, 0.0658, 0.1542,
    ];

    private const double StabilityMin = 0.001;
    private const double MinDifficulty = 1.0;
    private const double MaxDifficulty = 10.0;

    private readonly double[] _w;
    private readonly double _desiredRetention;
    private readonly int _maximumIntervalDays;
    private readonly IReadOnlyList<TimeSpan> _learningSteps;
    private readonly IReadOnlyList<TimeSpan> _relearningSteps;
    private readonly double _decay;
    private readonly double _factor;

    /// <summary>Creates a scheduler from the configured options and the default parameters.</summary>
    public FsrsScheduler(VocabularySchedulingOptions options)
    {
        _w = DefaultParameters.ToArray();
        _desiredRetention = options.DesiredRetention;
        _maximumIntervalDays = options.MaximumIntervalDays;
        _learningSteps = options.GetLearningSteps();
        _relearningSteps = options.GetRelearningSteps();
        _decay = -_w[20];
        _factor = Math.Pow(0.9, 1 / _decay) - 1;
    }

    /// <inheritdoc />
    public FsrsCard Schedule(FsrsCard card, FsrsRating rating, DateTime nowUtc)
    {
        (double stability, double difficulty) = NextMemoryState(card, rating, nowUtc);

        (FsrsPhase phase, int? step, TimeSpan interval) = card.Phase == FsrsPhase.Review
            ? NextFromReview(rating, stability)
            : NextFromSteps(
                card.Phase == FsrsPhase.Learning ? _learningSteps : _relearningSteps,
                card.Phase,
                card.Step ?? 0,
                rating,
                stability);

        return new FsrsCard(phase, step, stability, difficulty, nowUtc + interval, nowUtc);
    }

    private (double Stability, double Difficulty) NextMemoryState(FsrsCard card, FsrsRating rating, DateTime nowUtc)
    {
        if (card.LastReviewedAtUtc is not { } lastReviewedAtUtc)
        {
            return (InitialStability(rating), ClampDifficulty(InitialDifficulty(rating)));
        }

        int daysSinceLastReview = (int)Math.Floor((nowUtc - lastReviewedAtUtc).TotalDays);
        double stability = daysSinceLastReview < 1
            ? ShortTermStability(card.Stability, rating)
            : NextStability(card.Difficulty, card.Stability, Retrievability(card.Stability, daysSinceLastReview), rating);

        return (stability, NextDifficulty(card.Difficulty, rating));
    }

    private (FsrsPhase Phase, int? Step, TimeSpan Interval) NextFromReview(FsrsRating rating, double stability)
    {
        if (rating == FsrsRating.Again && _relearningSteps.Count > 0)
        {
            return (FsrsPhase.Relearning, 0, _relearningSteps[0]);
        }

        return (FsrsPhase.Review, null, TimeSpan.FromDays(NextIntervalDays(stability)));
    }

    private (FsrsPhase Phase, int? Step, TimeSpan Interval) NextFromSteps(
        IReadOnlyList<TimeSpan> steps,
        FsrsPhase phase,
        int step,
        FsrsRating rating,
        double stability)
    {
        if (steps.Count == 0 || (step >= steps.Count && rating != FsrsRating.Again))
        {
            return Graduate(stability);
        }

        switch (rating)
        {
            case FsrsRating.Again:
                return (phase, 0, steps[0]);

            case FsrsRating.Hard:
                if (step == 0 && steps.Count == 1)
                {
                    return (phase, step, steps[0] * 1.5);
                }

                if (step == 0)
                {
                    return (phase, step, (steps[0] + steps[1]) / 2.0);
                }

                return (phase, step, steps[step]);

            case FsrsRating.Good:
                if (step + 1 == steps.Count)
                {
                    return Graduate(stability);
                }

                return (phase, step + 1, steps[step + 1]);

            default:
                return Graduate(stability);
        }
    }

    private (FsrsPhase Phase, int? Step, TimeSpan Interval) Graduate(double stability) =>
        (FsrsPhase.Review, null, TimeSpan.FromDays(NextIntervalDays(stability)));

    private double Retrievability(double stability, int daysSinceLastReview)
    {
        int elapsedDays = Math.Max(0, daysSinceLastReview);
        return Math.Pow(1 + (_factor * elapsedDays / stability), _decay);
    }

    private double InitialStability(FsrsRating rating) => Math.Max(_w[(int)rating - 1], StabilityMin);

    private double InitialDifficulty(FsrsRating rating) => _w[4] - Math.Exp(_w[5] * ((int)rating - 1)) + 1;

    private static double ClampDifficulty(double difficulty) => Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);

    private int NextIntervalDays(double stability)
    {
        double interval = stability / _factor * (Math.Pow(_desiredRetention, 1 / _decay) - 1);
        int rounded = (int)Math.Round(interval, MidpointRounding.ToEven);
        return Math.Min(Math.Max(rounded, 1), _maximumIntervalDays);
    }

    private double ShortTermStability(double stability, FsrsRating rating)
    {
        double increase = Math.Exp(_w[17] * ((int)rating - 3 + _w[18])) * Math.Pow(stability, -_w[19]);
        if (rating != FsrsRating.Again)
        {
            increase = Math.Max(increase, 1.0);
        }

        return Math.Max(stability * increase, StabilityMin);
    }

    private double NextDifficulty(double difficulty, FsrsRating rating)
    {
        double easyInitial = InitialDifficulty(FsrsRating.Easy);
        double delta = -(_w[6] * ((int)rating - 3));
        double damped = difficulty + ((10.0 - difficulty) * delta / 9.0);
        double reverted = (_w[7] * easyInitial) + ((1 - _w[7]) * damped);
        return ClampDifficulty(reverted);
    }

    private double NextStability(double difficulty, double stability, double retrievability, FsrsRating rating)
    {
        double next = rating == FsrsRating.Again
            ? NextForgetStability(difficulty, stability, retrievability)
            : NextRecallStability(difficulty, stability, retrievability, rating);
        return Math.Max(next, StabilityMin);
    }

    private double NextForgetStability(double difficulty, double stability, double retrievability)
    {
        double longTerm = _w[11]
            * Math.Pow(difficulty, -_w[12])
            * (Math.Pow(stability + 1, _w[13]) - 1)
            * Math.Exp((1 - retrievability) * _w[14]);
        double shortTerm = stability / Math.Exp(_w[17] * _w[18]);
        return Math.Min(longTerm, shortTerm);
    }

    private double NextRecallStability(double difficulty, double stability, double retrievability, FsrsRating rating)
    {
        double hardPenalty = rating == FsrsRating.Hard ? _w[15] : 1;
        double easyBonus = rating == FsrsRating.Easy ? _w[16] : 1;
        return stability * (1
            + (Math.Exp(_w[8])
               * (11 - difficulty)
               * Math.Pow(stability, -_w[9])
               * (Math.Exp((1 - retrievability) * _w[10]) - 1)
               * hardPenalty
               * easyBonus));
    }
}
