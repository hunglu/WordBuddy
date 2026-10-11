using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>One learner in one challenge. Score and rank are a projection, rewritten by the scorer.</summary>
public sealed class ChallengeParticipant : Entity
{
    /// <summary>The challenge (plain field, no cross-service key).</summary>
    public Guid ChallengeId { get; private set; }

    /// <summary>The learner.</summary>
    public Guid UserId { get; private set; }

    /// <summary>When the learner joined, UTC. Only answers after this count.</summary>
    public DateTime JoinedAtUtc { get; private set; }

    /// <summary>Current score.</summary>
    public int Score { get; private set; }

    /// <summary>Dense rank (1 is best), or null before the first projection.</summary>
    public int? Rank { get; private set; }

    /// <summary>Whether the target is reached (target challenges).</summary>
    public bool TargetReached { get; private set; }

    private ChallengeParticipant(Guid id) : base(id)
    {
    }

    /// <summary>Creates a participant with score 0.</summary>
    public static ChallengeParticipant Create(Guid id, Guid challengeId, Guid userId, DateTime joinedAtUtc) =>
        new(id) { ChallengeId = challengeId, UserId = userId, JoinedAtUtc = joinedAtUtc };

    /// <summary>Rewrites the projection. Same input gives the same state (idempotent).</summary>
    public void ApplyProjection(int score, int rank, bool targetReached)
    {
        Score = score;
        Rank = rank;
        TargetReached = targetReached;
    }
}
