using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Infrastructure.Repositories;

internal sealed class LearnerGroupRepository : ILearnerGroupRepository
{
    private readonly IdentityDbContext _dbContext;
    private readonly ILogger<LearnerGroupRepository> _logger;

    public LearnerGroupRepository(IdentityDbContext dbContext, ILogger<LearnerGroupRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<LearnerGroup>> GetGroupTrackedAsync(Guid groupId, CancellationToken ct = default)
    {
        LearnerGroup? group = await _dbContext.LearnerGroups.AsTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId && g.DeletedAtUtc == null, ct);
        return group is null ? Result.Failure<LearnerGroup>(LearnerGroupErrors.NotFound) : Result.Success(group);
    }

    public async Task<Result<LearnerGroup>> GetGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        LearnerGroup? group = await _dbContext.LearnerGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.DeletedAtUtc == null, ct);
        return group is null ? Result.Failure<LearnerGroup>(LearnerGroupErrors.NotFound) : Result.Success(group);
    }

    public async Task<IReadOnlyList<LearnerGroup>> GetGroupsByIdsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default) =>
        await _dbContext.LearnerGroups.Where(g => groupIds.Contains(g.Id) && g.DeletedAtUtc == null).ToListAsync(ct);

    public Task<int> CountGroupsByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        _dbContext.LearnerGroups.CountAsync(g => g.OwnerId == ownerId && g.DeletedAtUtc == null, ct);

    public async Task<IReadOnlyList<LearnerGroup>> GetGroupsByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await _dbContext.LearnerGroups
            .Where(g => g.OwnerId == ownerId && g.DeletedAtUtc == null)
            .OrderBy(g => g.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LearnerGroupMember>> GetOpenMembersAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMembers
            .Where(m => groupIds.Contains(m.GroupId) && m.Status != GroupMemberStatus.Removed)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LearnerGroupMember>> GetOpenMembersTrackedAsync(Guid groupId, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMembers.AsTracking()
            .Where(m => m.GroupId == groupId && m.Status != GroupMemberStatus.Removed)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LearnerGroupMember>> GetActiveMembershipsOfLearnerAsync(Guid learnerId, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMembers
            .Where(m => m.LearnerId == learnerId && m.Status == GroupMemberStatus.Active)
            .ToListAsync(ct);

    public async Task<Result<LearnerGroupMember>> GetMemberTrackedAsync(Guid memberId, CancellationToken ct = default)
    {
        LearnerGroupMember? member = await _dbContext.LearnerGroupMembers.AsTracking().FirstOrDefaultAsync(m => m.Id == memberId, ct);
        return member is null ? Result.Failure<LearnerGroupMember>(LearnerGroupErrors.MemberNotFound) : Result.Success(member);
    }

    public async Task<Result<LearnerGroupMember>> GetOpenMemberTrackedAsync(Guid groupId, Guid learnerId, CancellationToken ct = default)
    {
        LearnerGroupMember? member = await _dbContext.LearnerGroupMembers.AsTracking()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.LearnerId == learnerId && m.Status != GroupMemberStatus.Removed, ct);
        return member is null ? Result.Failure<LearnerGroupMember>(LearnerGroupErrors.MemberNotFound) : Result.Success(member);
    }

    public async Task<IReadOnlyList<LearnerGroupMember>> GetOpenMembersOfOwnerAndLearnerTrackedAsync(
        Guid ownerId, Guid learnerId, CancellationToken ct = default)
    {
        IQueryable<Guid> ownerGroupIds = _dbContext.LearnerGroups
            .Where(g => g.OwnerId == ownerId && g.DeletedAtUtc == null)
            .Select(g => g.Id);

        return await _dbContext.LearnerGroupMembers.AsTracking()
            .Where(m => m.LearnerId == learnerId && m.Status != GroupMemberStatus.Removed && ownerGroupIds.Contains(m.GroupId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LearnerGroupMember>> GetPendingForPrimaryAsync(Guid primarySupporterId, CancellationToken ct = default)
    {
        IQueryable<Guid> childIds = _dbContext.SupportLinks
            .Where(l => l.SupporterId == primarySupporterId && l.IsPrimary && l.Status == SupportLinkStatus.Active)
            .Select(l => l.LearnerId);
        IQueryable<Guid> liveGroupIds = _dbContext.LearnerGroups.Where(g => g.DeletedAtUtc == null).Select(g => g.Id);

        return await _dbContext.LearnerGroupMembers
            .Where(m => m.Status == GroupMemberStatus.PendingPrimaryApproval
                && childIds.Contains(m.LearnerId)
                && liveGroupIds.Contains(m.GroupId))
            .OrderBy(m => m.AddedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddGroupAsync(LearnerGroup group, CancellationToken ct = default) =>
        await _dbContext.LearnerGroups.AddAsync(group, ct);

    public async Task AddMemberAsync(LearnerGroupMember member, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMembers.AddAsync(member, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // The filtered unique index (one open membership per learner and group) turns a race into a conflict.
            _logger.LogWarning(ex, "Group save failed with a database conflict");
            return Result.Failure(Error.Conflict("LearnerGroup.Conflict", "The change conflicts with another change. Please retry."));
        }
    }
}
