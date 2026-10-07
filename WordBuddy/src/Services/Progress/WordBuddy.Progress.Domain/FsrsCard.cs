namespace WordBuddy.Progress.Domain;

/// <summary>Immutable FSRS card state. <see cref="LastReviewedAtUtc"/> = <see langword="null"/> means never reviewed.</summary>
/// <param name="Phase">FSRS state.</param>
/// <param name="Step">Current (re)learning step; <see langword="null"/> in <see cref="FsrsPhase.Review"/>.</param>
/// <param name="Stability">Stability in days (0 before the first review).</param>
/// <param name="Difficulty">Difficulty 1–10 (0 before the first review).</param>
/// <param name="DueAtUtc">Next due time (UTC).</param>
/// <param name="LastReviewedAtUtc">Last review time (UTC).</param>
public sealed record FsrsCard(
    FsrsPhase Phase,
    int? Step,
    double Stability,
    double Difficulty,
    DateTime DueAtUtc,
    DateTime? LastReviewedAtUtc)
{
    /// <summary>A card that was never reviewed, due at <paramref name="dueAtUtc"/>.</summary>
    public static FsrsCard New(DateTime dueAtUtc) => new(FsrsPhase.Learning, 0, 0, 0, dueAtUtc, null);
}
