using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WordBuddy.Progress.Infrastructure.Persistence.Dialects;

/// <summary>PostgreSQL: SQLSTATE 23505 (unique violation), 40P01 (deadlock detected), row lock on the parent challenge.</summary>
internal sealed class PostgresDatabaseDialect : IDatabaseDialect
{
    /// <inheritdoc />
    public bool IsUniqueViolation(Exception exception) =>
        Find(exception) is { } pg && IsUnique(pg.SqlState);

    /// <inheritdoc />
    public bool IsDeadlock(Exception exception) =>
        Find(exception) is { } pg && IsDeadlockState(pg.SqlState);

    /// <inheritdoc />
    public async Task<int> CountParticipantsForJoinAsync(ProgressDbContext dbContext, Guid challengeId, CancellationToken ct)
    {
        // FOR UPDATE is not allowed with COUNT(*), and it does not lock gaps. Locking the parent Challenges row
        // serializes joins of one challenge: the next join waits here, then counts the committed participants.
        await dbContext.Database
            .SqlQuery<int>($"SELECT 1 AS \"Value\" FROM \"Challenges\" WHERE \"Id\" = {challengeId} FOR UPDATE")
            .ToListAsync(ct);

        return await dbContext.ChallengeParticipants.CountAsync(p => p.ChallengeId == challengeId, ct);
    }

    /// <summary>Classifies a SQLSTATE (also used by unit tests).</summary>
    internal static bool IsUnique(string? sqlState) => sqlState == PostgresErrorCodes.UniqueViolation;

    /// <summary>Classifies a SQLSTATE (also used by unit tests).</summary>
    internal static bool IsDeadlockState(string? sqlState) => sqlState == PostgresErrorCodes.DeadlockDetected;

    private static PostgresException? Find(Exception exception) =>
        exception as PostgresException ?? exception.InnerException as PostgresException;
}
