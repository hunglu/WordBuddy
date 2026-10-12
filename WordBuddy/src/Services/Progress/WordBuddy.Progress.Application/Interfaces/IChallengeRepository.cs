using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Stores challenges, their participants and the Guardian consents.</summary>
public interface IChallengeRepository
{
    /// <summary>Returns the challenge (read-only), or <see cref="ChallengeErrors.NotFound"/>.</summary>
    Task<Result<Challenge>> GetByIdAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>Returns the tracked challenge, or <see cref="ChallengeErrors.NotFound"/>.</summary>
    Task<Result<Challenge>> GetTrackedByIdAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>Open and Running challenges of one audience (read-only), soonest start first.</summary>
    Task<IReadOnlyList<Challenge>> GetActiveAsync(ChallengeAudience audience, CancellationToken ct = default);

    /// <summary>All challenges that are not Finished, tracked. Used by the life cycle.</summary>
    Task<IReadOnlyList<Challenge>> GetNotFinishedTrackedAsync(CancellationToken ct = default);

    /// <summary>All Running challenges, tracked. Used by the score projection.</summary>
    Task<IReadOnlyList<Challenge>> GetRunningTrackedAsync(CancellationToken ct = default);

    /// <summary>The instance of a template and audience with the latest start (read-only), or not found.</summary>
    Task<Result<Challenge>> GetLatestAsync(string templateKey, ChallengeAudience audience, CancellationToken ct = default);

    /// <summary>Stages a new challenge.</summary>
    Task AddAsync(Challenge challenge, CancellationToken ct = default);

    /// <summary>Participants of a challenge (read-only).</summary>
    Task<IReadOnlyList<ChallengeParticipant>> GetParticipantsAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>Participants of a challenge, tracked. Used by the score projection.</summary>
    Task<IReadOnlyList<ChallengeParticipant>> GetParticipantsTrackedAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>The tracked participant row of a learner in a challenge, or not found.</summary>
    Task<Result<ChallengeParticipant>> GetParticipantTrackedAsync(Guid challengeId, Guid userId, CancellationToken ct = default);

    /// <summary>Participant counts for the given challenges. Challenges without participants are missing.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetParticipantCountsAsync(IReadOnlyCollection<Guid> challengeIds, CancellationToken ct = default);

    /// <summary>The learner participant rows (read-only) in the given challenges.</summary>
    Task<IReadOnlyList<ChallengeParticipant>> GetParticipationsAsync(Guid userId, IReadOnlyCollection<Guid> challengeIds, CancellationToken ct = default);

    /// <summary>The learner participant rows (tracked) in challenges that are not Finished.</summary>
    Task<IReadOnlyList<ChallengeParticipant>> GetUnfinishedParticipationsTrackedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Adds the participant and saves, only when the challenge still has room (<paramref name="capacity"/>).
    /// The count and the insert run in one serializable transaction, so a full challenge never overflows.
    /// Returns <see cref="ChallengeErrors.Full"/> when there is no room.
    /// </summary>
    Task<Result> AddParticipantIfRoomAsync(ChallengeParticipant participant, int capacity, CancellationToken ct = default);

    /// <summary>Stages removal of a participant.</summary>
    void RemoveParticipant(ChallengeParticipant participant);

    /// <summary>Whether at least one Guardian consent exists for the child.</summary>
    Task<bool> HasConsentAsync(Guid childId, CancellationToken ct = default);

    /// <summary>Tracked consents filtered by child and/or guardian (null = no filter).</summary>
    Task<IReadOnlyList<ChallengeConsent>> GetConsentsTrackedAsync(Guid? childId, Guid? enabledBy, CancellationToken ct = default);

    /// <summary>Stages a new consent.</summary>
    Task AddConsentAsync(ChallengeConsent consent, CancellationToken ct = default);

    /// <summary>Stages removal of consents.</summary>
    void RemoveConsents(IEnumerable<ChallengeConsent> consents);

    /// <summary>Saves staged changes.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
