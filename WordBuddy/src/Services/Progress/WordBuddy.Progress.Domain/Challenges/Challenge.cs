using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>
/// A system-created vocabulary challenge. The window is the same UTC instants for everyone.
/// Status moves forward only. Scores are a projection of <see cref="ReviewLog"/> and live on
/// <see cref="ChallengeParticipant"/>.
/// </summary>
public sealed class Challenge : Entity
{
    /// <summary>Key of the <see cref="ChallengeTemplate"/> this instance came from.</summary>
    public string TemplateKey { get; private set; } = string.Empty;

    /// <summary>Who may join.</summary>
    public ChallengeAudience Audience { get; private set; }

    /// <summary>Ranked or target.</summary>
    public ChallengeKind Kind { get; private set; }

    /// <summary>What is counted.</summary>
    public ChallengeMetric Metric { get; private set; }

    /// <summary>Target to reach (target challenges), otherwise null.</summary>
    public int? Target { get; private set; }

    /// <summary>When joining opens, UTC.</summary>
    public DateTime OpensAtUtc { get; private set; }

    /// <summary>When counting starts, UTC.</summary>
    public DateTime StartsAtUtc { get; private set; }

    /// <summary>When counting ends (exclusive), UTC.</summary>
    public DateTime EndsAtUtc { get; private set; }

    /// <summary>Most participants.</summary>
    public int Capacity { get; private set; }

    /// <summary>Current life cycle status.</summary>
    public ChallengeStatus Status { get; private set; }

    private Challenge(Guid id) : base(id)
    {
    }

    /// <summary>Creates a scheduled challenge. Fails with <see cref="ChallengeErrors.InvalidSchedule"/> for a bad schedule, capacity or target.</summary>
    public static Result<Challenge> Create(
        Guid id,
        string templateKey,
        ChallengeAudience audience,
        ChallengeKind kind,
        ChallengeMetric metric,
        int? target,
        DateTime opensAtUtc,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        int capacity)
    {
        bool scheduleOk = opensAtUtc <= startsAtUtc && startsAtUtc < endsAtUtc;
        bool targetOk = kind == ChallengeKind.Ranked ? target is null : target is > 0;
        if (string.IsNullOrWhiteSpace(templateKey) || capacity < 1 || !scheduleOk || !targetOk)
        {
            return Result.Failure<Challenge>(ChallengeErrors.InvalidSchedule);
        }

        return Result.Success(new Challenge(id)
        {
            TemplateKey = templateKey,
            Audience = audience,
            Kind = kind,
            Metric = metric,
            Target = target,
            OpensAtUtc = opensAtUtc,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            Capacity = capacity,
            Status = ChallengeStatus.Scheduled,
        });
    }

    /// <summary>Scheduled to Open.</summary>
    public Result Open() => MoveTo(ChallengeStatus.Scheduled, ChallengeStatus.Open);

    /// <summary>Open to Running.</summary>
    public Result Start() => MoveTo(ChallengeStatus.Open, ChallengeStatus.Running);

    /// <summary>Running to Finished.</summary>
    public Result Finish() => MoveTo(ChallengeStatus.Running, ChallengeStatus.Finished);

    /// <summary>
    /// Whether a caller of <paramref name="callerAudience"/> may join now, ignoring capacity and consent.
    /// Audience first, then status (Open or Running).
    /// </summary>
    public Result CheckJoinable(ChallengeAudience callerAudience)
    {
        if (callerAudience != Audience)
        {
            return Result.Failure(ChallengeErrors.AudienceMismatch);
        }

        return Status is ChallengeStatus.Open or ChallengeStatus.Running
            ? Result.Success()
            : Result.Failure(ChallengeErrors.NotOpen);
    }

    /// <summary>Whether another participant fits, given the current count.</summary>
    public bool HasRoom(int participantCount) => participantCount < Capacity;

    private Result MoveTo(ChallengeStatus from, ChallengeStatus to)
    {
        if (Status != from)
        {
            return Result.Failure(ChallengeErrors.InvalidTransition);
        }

        Status = to;
        return Result.Success();
    }
}
