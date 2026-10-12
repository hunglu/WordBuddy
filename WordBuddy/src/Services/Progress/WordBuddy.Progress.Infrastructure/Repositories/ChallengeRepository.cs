using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class ChallengeRepository : IChallengeRepository
{
    private readonly ProgressDbContext _dbContext;
    private readonly ILogger<ChallengeRepository> _logger;

    public ChallengeRepository(ProgressDbContext dbContext, ILogger<ChallengeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Challenge>> GetByIdAsync(Guid challengeId, CancellationToken ct = default)
    {
        Challenge? challenge = await _dbContext.Challenges.FirstOrDefaultAsync(c => c.Id == challengeId, ct);
        return challenge is null ? Result.Failure<Challenge>(ChallengeErrors.NotFound) : Result.Success(challenge);
    }

    public async Task<Result<Challenge>> GetTrackedByIdAsync(Guid challengeId, CancellationToken ct = default)
    {
        Challenge? challenge = await _dbContext.Challenges.AsTracking().FirstOrDefaultAsync(c => c.Id == challengeId, ct);
        return challenge is null ? Result.Failure<Challenge>(ChallengeErrors.NotFound) : Result.Success(challenge);
    }

    public async Task<IReadOnlyList<Challenge>> GetActiveAsync(ChallengeAudience audience, CancellationToken ct = default) =>
        await _dbContext.Challenges
            .Where(c => c.Audience == audience && (c.Status == ChallengeStatus.Open || c.Status == ChallengeStatus.Running))
            .OrderBy(c => c.StartsAtUtc).ThenBy(c => c.TemplateKey)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Challenge>> GetNotFinishedTrackedAsync(CancellationToken ct = default) =>
        await _dbContext.Challenges.AsTracking().Where(c => c.Status != ChallengeStatus.Finished).OrderBy(c => c.StartsAtUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<Challenge>> GetRunningTrackedAsync(CancellationToken ct = default) =>
        await _dbContext.Challenges.AsTracking().Where(c => c.Status == ChallengeStatus.Running).ToListAsync(ct);

    public async Task<Result<Challenge>> GetLatestAsync(string templateKey, ChallengeAudience audience, CancellationToken ct = default)
    {
        Challenge? challenge = await _dbContext.Challenges
            .Where(c => c.TemplateKey == templateKey && c.Audience == audience)
            .OrderByDescending(c => c.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
        return challenge is null ? Result.Failure<Challenge>(ChallengeErrors.NotFound) : Result.Success(challenge);
    }

    public async Task AddAsync(Challenge challenge, CancellationToken ct = default) =>
        await _dbContext.Challenges.AddAsync(challenge, ct);

    public async Task<IReadOnlyList<ChallengeParticipant>> GetParticipantsAsync(Guid challengeId, CancellationToken ct = default) =>
        await _dbContext.ChallengeParticipants.Where(p => p.ChallengeId == challengeId).ToListAsync(ct);

    public async Task<IReadOnlyList<ChallengeParticipant>> GetParticipantsTrackedAsync(Guid challengeId, CancellationToken ct = default) =>
        await _dbContext.ChallengeParticipants.AsTracking().Where(p => p.ChallengeId == challengeId).ToListAsync(ct);

    public async Task<Result<ChallengeParticipant>> GetParticipantTrackedAsync(Guid challengeId, Guid userId, CancellationToken ct = default)
    {
        ChallengeParticipant? participant = await _dbContext.ChallengeParticipants.AsTracking()
            .FirstOrDefaultAsync(p => p.ChallengeId == challengeId && p.UserId == userId, ct);
        return participant is null
            ? Result.Failure<ChallengeParticipant>(Error.NotFound("Challenge.NotJoined", "The learner has not joined this challenge."))
            : Result.Success(participant);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetParticipantCountsAsync(IReadOnlyCollection<Guid> challengeIds, CancellationToken ct = default) =>
        await _dbContext.ChallengeParticipants
            .Where(p => challengeIds.Contains(p.ChallengeId))
            .GroupBy(p => p.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ChallengeId, x => x.Count, ct);

    public async Task<IReadOnlyList<ChallengeParticipant>> GetParticipationsAsync(Guid userId, IReadOnlyCollection<Guid> challengeIds, CancellationToken ct = default) =>
        await _dbContext.ChallengeParticipants.Where(p => p.UserId == userId && challengeIds.Contains(p.ChallengeId)).ToListAsync(ct);

    public async Task<IReadOnlyList<ChallengeParticipant>> GetUnfinishedParticipationsTrackedAsync(Guid userId, CancellationToken ct = default) =>
        await _dbContext.ChallengeParticipants.AsTracking()
            .Where(p => p.UserId == userId
                && _dbContext.Challenges.Any(c => c.Id == p.ChallengeId && c.Status != ChallengeStatus.Finished))
            .ToListAsync(ct);

    private const int DeadlockVictimNumber = 1205;
    private const int MaxJoinAttempts = 4;

    public async Task<Result> AddParticipantIfRoomAsync(ChallengeParticipant participant, int capacity, CancellationToken ct = default)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await TryAddParticipantIfRoomAsync(participant, capacity, ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // The unique (ChallengeId, UserId) index: the learner joined twice at once.
                _logger.LogWarning(ex, "ChallengeParticipant insert hit the unique index");
                _dbContext.Entry(participant).State = EntityState.Detached;
                return Result.Failure(Error.Conflict("Challenge.AlreadyJoined", "The learner has already joined this challenge."));
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException)
            {
                // Deadlock victim, timeout or similar: the transaction rolled back, so the learner is NOT joined.
                _dbContext.Entry(participant).State = EntityState.Detached;
                bool deadlock = (ex as SqlException ?? ex.InnerException as SqlException)?.Number == DeadlockVictimNumber;
                if (deadlock && attempt < MaxJoinAttempts)
                {
                    _logger.LogWarning(ex, "ChallengeParticipant join was a deadlock victim, retrying: Attempt={Attempt}", attempt);
                    continue;
                }

                _logger.LogWarning(ex, "ChallengeParticipant join failed with a retryable database error");
                return Result.Failure(Error.Conflict("Challenge.Conflict", "A concurrent challenge update conflicted. Try again."));
            }
        }
    }

    private async Task<Result> TryAddParticipantIfRoomAsync(ChallengeParticipant participant, int capacity, CancellationToken ct)
    {
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        // UPDLOCK + HOLDLOCK takes an exclusive key-range lock: concurrent joins of one challenge queue up here
        // instead of both holding shared locks and deadlocking on the insert. The index hint forces a seek on
        // (ChallengeId, UserId), so the range lock covers only this challenge, whatever the optimizer thinks of a tiny table.
        int count = await _dbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [ChallengeParticipants] WITH (INDEX([IX_ChallengeParticipants_ChallengeId_UserId]), UPDLOCK, HOLDLOCK) WHERE [ChallengeId] = {participant.ChallengeId}")
            .SingleAsync(ct);
        if (count >= capacity)
        {
            return Result.Failure(ChallengeErrors.Full);
        }

        await _dbContext.ChallengeParticipants.AddAsync(participant, ct);
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    public void RemoveParticipant(ChallengeParticipant participant) => _dbContext.ChallengeParticipants.Remove(participant);

    public Task<bool> HasConsentAsync(Guid childId, CancellationToken ct = default) =>
        _dbContext.ChallengeConsents.AnyAsync(c => c.ChildId == childId, ct);

    public async Task<IReadOnlyList<ChallengeConsent>> GetConsentsTrackedAsync(Guid? childId, Guid? enabledBy, CancellationToken ct = default) =>
        await _dbContext.ChallengeConsents.AsTracking()
            .Where(c => (childId == null || c.ChildId == childId) && (enabledBy == null || c.EnabledBy == enabledBy))
            .ToListAsync(ct);

    public async Task AddConsentAsync(ChallengeConsent consent, CancellationToken ct = default) =>
        await _dbContext.ChallengeConsents.AddAsync(consent, ct);

    public void RemoveConsents(IEnumerable<ChallengeConsent> consents) => _dbContext.ChallengeConsents.RemoveRange(consents);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Challenge save failed with a database conflict");
            return Result.Failure(Error.Conflict("Challenge.Conflict", "A concurrent challenge update conflicted."));
        }
    }
}
