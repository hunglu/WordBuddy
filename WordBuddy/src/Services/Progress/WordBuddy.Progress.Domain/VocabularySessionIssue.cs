namespace WordBuddy.Progress.Domain;

/// <summary>One planned word of a session: sense id, new or due, and its place in the session.</summary>
public sealed class VocabularySessionIssueItem
{
    /// <summary>Owning session id.</summary>
    public Guid SessionId { get; }

    /// <summary>Content's sense id.</summary>
    public Guid SenseId { get; }

    /// <summary><see langword="true"/> for a new word, <see langword="false"/> for a due word.</summary>
    public bool IsNew { get; }

    /// <summary>0-based order inside the session (due first, then new).</summary>
    public int Position { get; }

    internal VocabularySessionIssueItem(Guid sessionId, Guid senseId, bool isNew, int position)
    {
        SessionId = sessionId;
        SenseId = senseId;
        IsNew = isNew;
        Position = position;
    }
}

/// <summary>
/// One vocabulary session handed to a learner. Ids, time and the planned items only — no word
/// text. A session has a duration and an expiry: while it is open, a reload resumes it. The only
/// change after creation is <see cref="End"/>. The dashboard compares <see cref="PlannedCount"/>
/// with the answers logged for the session.
/// </summary>
public sealed class VocabularySessionIssue
{
    private readonly List<VocabularySessionIssueItem> _items = [];

    /// <summary>Session id sent to the client (primary key).</summary>
    public Guid SessionId { get; }

    /// <summary>The learner's user id (plain field, not a foreign key).</summary>
    public Guid UserId { get; }

    /// <summary>Server time the session was issued (UTC).</summary>
    public DateTime IssuedAtUtc { get; }

    /// <summary>Number of items in the session (due + new).</summary>
    public int PlannedCount { get; }

    /// <summary>Configured session length in minutes.</summary>
    public int DurationMinutes { get; }

    /// <summary>End of the session: the earlier of issued + duration and the end of the learner's local day (UTC).</summary>
    public DateTime ExpiresAtUtc { get; }

    /// <summary>Time the session was closed because every item was answered (UTC), or <see langword="null"/>.</summary>
    public DateTime? EndedAtUtc { get; private set; }

    /// <summary>Planned items in session order.</summary>
    public IReadOnlyList<VocabularySessionIssueItem> Items => _items;

    private VocabularySessionIssue(
        Guid sessionId, Guid userId, DateTime issuedAtUtc, int plannedCount, int durationMinutes, DateTime expiresAtUtc)
    {
        SessionId = sessionId;
        UserId = userId;
        IssuedAtUtc = issuedAtUtc;
        PlannedCount = plannedCount;
        DurationMinutes = durationMinutes;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Creates a session issue. <c>ExpiresAtUtc = min(issuedAtUtc + duration, localDayEndUtc)</c>.
    /// <see cref="PlannedCount"/> is the number of <paramref name="items"/>.
    /// </summary>
    public static VocabularySessionIssue Create(
        Guid sessionId,
        Guid userId,
        DateTime issuedAtUtc,
        IReadOnlyList<(Guid SenseId, bool IsNew)> items,
        int durationMinutes,
        DateTime localDayEndUtc)
    {
        DateTime byDuration = issuedAtUtc.AddMinutes(durationMinutes);
        DateTime expires = byDuration < localDayEndUtc ? byDuration : localDayEndUtc;

        VocabularySessionIssue issue = new(sessionId, userId, issuedAtUtc, items.Count, durationMinutes, expires);
        for (int i = 0; i < items.Count; i++)
        {
            issue._items.Add(new VocabularySessionIssueItem(sessionId, items[i].SenseId, items[i].IsNew, i));
        }

        return issue;
    }

    /// <summary>True while the session is not ended and <paramref name="nowUtc"/> is before <see cref="ExpiresAtUtc"/>.</summary>
    public bool IsOpen(DateTime nowUtc) => EndedAtUtc is null && nowUtc < ExpiresAtUtc;

    /// <summary>Closes the session. Call once all items are answered.</summary>
    public void End(DateTime nowUtc) => EndedAtUtc ??= nowUtc;
}
