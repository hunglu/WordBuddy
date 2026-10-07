namespace WordBuddy.Progress.Domain;

/// <summary>Computes the next FSRS card state. Pure: no I/O, no randomness.</summary>
public interface IFsrsScheduler
{
    /// <summary>Applies one review with <paramref name="rating"/> at <paramref name="nowUtc"/>.</summary>
    FsrsCard Schedule(FsrsCard card, FsrsRating rating, DateTime nowUtc);
}
