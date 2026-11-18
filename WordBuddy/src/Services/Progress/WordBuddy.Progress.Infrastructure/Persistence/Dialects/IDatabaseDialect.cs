namespace WordBuddy.Progress.Infrastructure.Persistence.Dialects;

/// <summary>
/// The few places where repositories must know the database engine: error classification and the
/// row lock used by challenge joins. Registered once per provider.
/// </summary>
internal interface IDatabaseDialect
{
    /// <summary>True when <paramref name="exception"/> (or its inner exception) is a duplicate-key error.</summary>
    /// <param name="exception">The caught exception, usually a <c>DbUpdateException</c>.</param>
    bool IsUniqueViolation(Exception exception);

    /// <summary>True when <paramref name="exception"/> (or its inner exception) means the transaction was chosen as a deadlock victim.</summary>
    /// <param name="exception">The caught exception.</param>
    bool IsDeadlock(Exception exception);

    /// <summary>
    /// Counts the participants of a challenge while holding a lock that serializes concurrent joins of the
    /// same challenge until the current transaction ends. Must run inside a transaction.
    /// </summary>
    /// <param name="dbContext">The context that owns the open transaction.</param>
    /// <param name="challengeId">The challenge being joined.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<int> CountParticipantsForJoinAsync(ProgressDbContext dbContext, Guid challengeId, CancellationToken ct);
}
