using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class LearnerWordMembershipRepository : ILearnerWordMembershipRepository
{
    private readonly ProgressDbContext _dbContext;
    private readonly ILogger<LearnerWordMembershipRepository> _logger;

    public LearnerWordMembershipRepository(ProgressDbContext dbContext, ILogger<LearnerWordMembershipRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<Result<bool>> UpsertAddedAsync(
        Guid userId,
        Guid senseId,
        Guid addedBy,
        DateTime addedAtUtc,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Upserting added LearnerWordMembership: UserId={UserId}, SenseId={SenseId}", userId, senseId);

        return UpsertAsync(
            userId,
            senseId,
            () => LearnerWordMembership.CreateAdded(Guid.NewGuid(), userId, senseId, addedBy, addedAtUtc),
            existing => existing.RecordAdded(addedBy, addedAtUtc),
            beforeSave,
            ct);
    }

    public Task<Result<bool>> MarkRemovedAsync(
        Guid userId,
        Guid senseId,
        DateTime removedAtUtc,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Marking LearnerWordMembership removed: UserId={UserId}, SenseId={SenseId}", userId, senseId);

        return UpsertAsync(
            userId,
            senseId,
            () => LearnerWordMembership.CreateRemoved(Guid.NewGuid(), userId, senseId, removedAtUtc),
            existing => existing.RecordRemoved(removedAtUtc),
            beforeSave,
            ct);
    }

    private async Task<Result<bool>> UpsertAsync(
        Guid userId,
        Guid senseId,
        Func<LearnerWordMembership> create,
        Func<LearnerWordMembership, Result<bool>> apply,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct)
    {
        LearnerWordMembership? existing = await FindTrackedAsync(userId, senseId, ct);
        if (existing is not null)
        {
            return await ApplyAndSaveAsync(existing, apply, beforeSave, ct);
        }

        LearnerWordMembership created = create();
        await _dbContext.LearnerWordMemberships.AddAsync(created, ct);

        Result hookResult = await RunHookAsync(created, beforeSave, ct);
        if (hookResult.IsFailure)
        {
            DetachPendingInserts();
            return Result.Failure<bool>(hookResult.Error);
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success(true);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent event for the same (UserId, SenseId) inserted first
            // (IX_LearnerWordMemberships_UserId_SenseId) — apply to that row instead. The states
            // staged by the hook go too; the hook runs again against the winner.
            DetachPendingInserts();
            _logger.LogInformation(
                "Concurrent membership insert resolved to existing row: UserId={UserId}, SenseId={SenseId}", userId, senseId);
        }

        LearnerWordMembership? winner = await FindTrackedAsync(userId, senseId, ct);
        if (winner is null)
        {
            return Result.Failure<bool>(Error.Conflict(
                "LearnerWordMembership.ConcurrentUpdate", "The membership was changed concurrently."));
        }

        return await ApplyAndSaveAsync(winner, apply, beforeSave, ct);
    }

    private async Task<Result<bool>> ApplyAndSaveAsync(
        LearnerWordMembership membership,
        Func<LearnerWordMembership, Result<bool>> apply,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct)
    {
        Result<bool> applied = apply(membership);
        if (applied.IsFailure || !applied.Value)
        {
            return applied;
        }

        Result hookResult = await RunHookAsync(membership, beforeSave, ct);
        if (hookResult.IsFailure)
        {
            return Result.Failure<bool>(hookResult.Error);
        }

        await _dbContext.SaveChangesAsync(ct);
        return applied;
    }

    private static Task<Result> RunHookAsync(
        LearnerWordMembership membership,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct) =>
        beforeSave is null ? Task.FromResult(Result.Success()) : beforeSave(membership, ct);

    private void DetachPendingInserts()
    {
        foreach (EntityEntry entry in _dbContext.ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Added && e.Entity is LearnerWordMembership or LearnerWordState)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private Task<LearnerWordMembership?> FindTrackedAsync(Guid userId, Guid senseId, CancellationToken ct) =>
        _dbContext.LearnerWordMemberships
            .AsTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.SenseId == senseId, ct);

    /// <summary>SQL Server 2601 (unique index) / 2627 (unique constraint) duplicate-key errors.</summary>
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
