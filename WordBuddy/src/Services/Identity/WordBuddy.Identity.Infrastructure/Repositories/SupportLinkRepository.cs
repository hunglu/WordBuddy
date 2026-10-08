using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Infrastructure.Repositories;

internal sealed class SupportLinkRepository : ISupportLinkRepository
{
    private readonly IdentityDbContext _dbContext;
    private readonly ILogger<SupportLinkRepository> _logger;

    public SupportLinkRepository(IdentityDbContext dbContext, ILogger<SupportLinkRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<SupportLink>> GetLinkTrackedAsync(Guid linkId, CancellationToken ct = default)
    {
        SupportLink? link = await _dbContext.SupportLinks.AsTracking().FirstOrDefaultAsync(l => l.Id == linkId, ct);
        return link is null ? Result.Failure<SupportLink>(SupportLinkErrors.LinkNotFound) : Result.Success(link);
    }

    public async Task<IReadOnlyList<SupportLink>> GetLearnerLinksTrackedAsync(Guid learnerId, CancellationToken ct = default) =>
        await _dbContext.SupportLinks.AsTracking().Where(l => l.LearnerId == learnerId).ToListAsync(ct);

    public async Task<IReadOnlyList<SupportLink>> GetLinksForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _dbContext.SupportLinks
            .Where(l => (l.LearnerId == userId || l.SupporterId == userId) && l.Status != SupportLinkStatus.Revoked)
            .OrderBy(l => l.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SupportLink>> GetLearnerLinksAsync(Guid learnerId, CancellationToken ct = default) =>
        await _dbContext.SupportLinks.Where(l => l.LearnerId == learnerId).OrderBy(l => l.CreatedAtUtc).ToListAsync(ct);

    public Task<bool> HasActiveSupporterAsync(Guid learnerId, CancellationToken ct = default) =>
        _dbContext.SupportLinks.AnyAsync(l => l.LearnerId == learnerId && l.Status == SupportLinkStatus.Active, ct);

    public async Task<Guid?> GetActivePrimarySupporterIdAsync(Guid learnerId, CancellationToken ct = default) =>
        await _dbContext.SupportLinks
            .Where(l => l.LearnerId == learnerId && l.IsPrimary && l.Status == SupportLinkStatus.Active)
            .Select(l => (Guid?)l.SupporterId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SupportLink>> GetPendingForPrimaryAsync(Guid primarySupporterId, CancellationToken ct = default)
    {
        IQueryable<Guid> learnerIds = _dbContext.SupportLinks
            .Where(l => l.SupporterId == primarySupporterId && l.IsPrimary && l.Status == SupportLinkStatus.Active)
            .Select(l => l.LearnerId);

        return await _dbContext.SupportLinks
            .Where(l => l.Status == SupportLinkStatus.PendingPrimaryApproval && learnerIds.Contains(l.LearnerId))
            .OrderBy(l => l.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddLinkAsync(SupportLink link, CancellationToken ct = default) =>
        await _dbContext.SupportLinks.AddAsync(link, ct);

    public async Task<Result<SupportLinkInvitation>> GetInvitationByHashTrackedAsync(string? codeHash, string? tokenHash, CancellationToken ct = default)
    {
        SupportLinkInvitation? invitation = null;
        if (tokenHash is not null)
        {
            invitation = await _dbContext.SupportLinkInvitations.AsTracking().FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);
        }
        else if (codeHash is not null)
        {
            invitation = await _dbContext.SupportLinkInvitations.AsTracking().FirstOrDefaultAsync(i => i.CodeHash == codeHash, ct);
        }

        return invitation is null
            ? Result.Failure<SupportLinkInvitation>(SupportLinkErrors.InvitationNotFound)
            : Result.Success(invitation);
    }

    public async Task<Result<SupportLinkInvitation>> GetInvitationTrackedAsync(Guid invitationId, CancellationToken ct = default)
    {
        SupportLinkInvitation? invitation = await _dbContext.SupportLinkInvitations.AsTracking()
            .FirstOrDefaultAsync(i => i.Id == invitationId, ct);
        return invitation is null
            ? Result.Failure<SupportLinkInvitation>(SupportLinkErrors.InvitationNotFound)
            : Result.Success(invitation);
    }

    public async Task<IReadOnlyList<SupportLinkInvitation>> GetOpenInvitationsByCreatorAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default) =>
        await _dbContext.SupportLinkInvitations
            .Where(i => i.CreatedById == userId && i.Status == InvitationStatus.Pending && i.ExpiresAtUtc > nowUtc)
            .OrderBy(i => i.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task AddInvitationAsync(SupportLinkInvitation invitation, CancellationToken ct = default) =>
        await _dbContext.SupportLinkInvitations.AddAsync(invitation, ct);

    public async Task<Result<UnlinkRequest>> GetOpenUnlinkRequestTrackedAsync(Guid linkId, CancellationToken ct = default)
    {
        UnlinkRequest? request = await _dbContext.UnlinkRequests.AsTracking()
            .FirstOrDefaultAsync(
                r => r.LinkId == linkId && (r.Status == UnlinkRequestStatus.Pending || r.Status == UnlinkRequestStatus.OverrideRequested),
                ct);
        return request is null ? Result.Failure<UnlinkRequest>(SupportLinkErrors.UnlinkRequestNotFound) : Result.Success(request);
    }

    public async Task<Result<UnlinkRequest>> GetUnlinkRequestTrackedAsync(Guid unlinkRequestId, CancellationToken ct = default)
    {
        UnlinkRequest? request = await _dbContext.UnlinkRequests.AsTracking().FirstOrDefaultAsync(r => r.Id == unlinkRequestId, ct);
        return request is null ? Result.Failure<UnlinkRequest>(SupportLinkErrors.UnlinkRequestNotFound) : Result.Success(request);
    }

    public async Task<IReadOnlyList<UnlinkRequest>> GetOpenUnlinkRequestsAsync(IReadOnlyCollection<Guid> linkIds, CancellationToken ct = default) =>
        await _dbContext.UnlinkRequests
            .Where(r => linkIds.Contains(r.LinkId)
                && (r.Status == UnlinkRequestStatus.Pending || r.Status == UnlinkRequestStatus.OverrideRequested))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UnlinkRequest>> GetEscalatedUnlinkRequestsAsync(CancellationToken ct = default) =>
        await _dbContext.UnlinkRequests
            .Where(r => r.Status == UnlinkRequestStatus.OverrideRequested)
            .OrderBy(r => r.EscalatedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SupportLink>> GetLinksByIdsAsync(IReadOnlyCollection<Guid> linkIds, CancellationToken ct = default) =>
        await _dbContext.SupportLinks.Where(l => linkIds.Contains(l.Id)).ToListAsync(ct);

    public async Task AddUnlinkRequestAsync(UnlinkRequest request, CancellationToken ct = default) =>
        await _dbContext.UnlinkRequests.AddAsync(request, ct);

    public async Task AddAuditEntryAsync(SupportLinkAuditEntry entry, CancellationToken ct = default) =>
        await _dbContext.SupportLinkAuditEntries.AddAsync(entry, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // Unique filtered indexes (one active Primary, one open unlink request, code/token
            // hash) turn races into a conflict instead of a 500.
            _logger.LogWarning(ex, "Support link save failed with a database conflict");
            return Result.Failure(Error.Conflict("SupportLink.Conflict", "The change conflicts with another change. Please retry."));
        }
    }

    public async Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> work, CancellationToken ct = default)
    {
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        Result result = await work(ct);
        if (result.IsFailure)
        {
            await transaction.RollbackAsync(ct);
            return result;
        }

        await transaction.CommitAsync(ct);
        return result;
    }
}
