namespace WordBuddy.Progress.Domain;

/// <summary>
/// One vocabulary session handed to a learner. Insert-only: created through <see cref="Create"/>,
/// no setters and no update methods. Ids, time and the planned size only — no word text. The
/// dashboard compares <see cref="PlannedCount"/> with the answers logged for the session.
/// </summary>
public sealed class VocabularySessionIssue
{
    /// <summary>Session id sent to the client (primary key).</summary>
    public Guid SessionId { get; }

    /// <summary>The learner's user id (plain field, not a foreign key).</summary>
    public Guid UserId { get; }

    /// <summary>Server time the session was issued (UTC).</summary>
    public DateTime IssuedAtUtc { get; }

    /// <summary>Number of items in the session (due + new).</summary>
    public int PlannedCount { get; }

    private VocabularySessionIssue(Guid sessionId, Guid userId, DateTime issuedAtUtc, int plannedCount)
    {
        SessionId = sessionId;
        UserId = userId;
        IssuedAtUtc = issuedAtUtc;
        PlannedCount = plannedCount;
    }

    /// <summary>Creates a session issue row.</summary>
    public static VocabularySessionIssue Create(Guid sessionId, Guid userId, DateTime issuedAtUtc, int plannedCount) =>
        new(sessionId, userId, issuedAtUtc, plannedCount);
}
