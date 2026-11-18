using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Progress.Infrastructure.Persistence.Dialects;

/// <summary>SQL Server: error 2601/2627 (duplicate key), 1205 (deadlock victim), key-range lock hints.</summary>
internal sealed class SqlServerDatabaseDialect : IDatabaseDialect
{
    /// <inheritdoc />
    public bool IsUniqueViolation(Exception exception) =>
        Find(exception) is { } sql && IsUnique(sql.Number);

    /// <inheritdoc />
    public bool IsDeadlock(Exception exception) =>
        Find(exception) is { } sql && IsDeadlockNumber(sql.Number);

    /// <inheritdoc />
    public async Task<int> CountParticipantsForJoinAsync(ProgressDbContext dbContext, Guid challengeId, CancellationToken ct)
    {
        // UPDLOCK + HOLDLOCK takes an exclusive key-range lock: concurrent joins of one challenge queue up here
        // instead of both holding shared locks and deadlocking on the insert. The index hint forces a seek on
        // (ChallengeId, UserId), so the range lock covers only this challenge, whatever the optimizer thinks of a tiny table.
        return await dbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [ChallengeParticipants] WITH (INDEX([IX_ChallengeParticipants_ChallengeId_UserId]), UPDLOCK, HOLDLOCK) WHERE [ChallengeId] = {challengeId}")
            .SingleAsync(ct);
    }

    /// <summary>Classifies a SQL Server error number (also used by unit tests).</summary>
    internal static bool IsUnique(int number) => number is 2601 or 2627;

    /// <summary>Classifies a SQL Server error number (also used by unit tests).</summary>
    internal static bool IsDeadlockNumber(int number) => number == 1205;

    private static SqlException? Find(Exception exception) =>
        exception as SqlException ?? exception.InnerException as SqlException;
}
