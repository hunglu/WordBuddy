using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class GroupWordRepository : IGroupWordRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<GroupWordRepository> _logger;

    public GroupWordRepository(ContentDbContext dbContext, ILogger<GroupWordRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Sense>> GetSensesAsync(IReadOnlyCollection<Guid> senseIds, CancellationToken ct = default) =>
        await _dbContext.Senses.Where(s => senseIds.Contains(s.Id)).ToListAsync(ct);

    public async Task<IReadOnlySet<(Guid UserId, Guid SenseId)>> GetExistingLinksAsync(
        IReadOnlyCollection<Guid> userIds, IReadOnlyCollection<Guid> senseIds, CancellationToken ct = default)
    {
        List<(Guid UserId, Guid SenseId)> pairs = (await _dbContext.LearnerWords
                .Where(l => userIds.Contains(l.UserId) && senseIds.Contains(l.SenseId))
                .Select(l => new { l.UserId, l.SenseId })
                .ToListAsync(ct))
            .Select(l => (l.UserId, l.SenseId))
            .ToList();
        return pairs.ToHashSet();
    }

    public async Task<IReadOnlyList<GroupWordAssignment>> GetAssignmentsAsync(Guid groupId, CancellationToken ct = default) =>
        await _dbContext.GroupWordAssignments
            .Where(a => a.GroupId == groupId)
            .OrderByDescending(a => a.AssignedAtUtc)
            .ToListAsync(ct);

    public async Task AddLinksAsync(IReadOnlyCollection<LearnerWord> links, CancellationToken ct = default) =>
        await _dbContext.LearnerWords.AddRangeAsync(links, ct);

    public async Task AddAssignmentsAsync(IReadOnlyCollection<GroupWordAssignment> assignments, CancellationToken ct = default) =>
        await _dbContext.GroupWordAssignments.AddRangeAsync(assignments, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // The unique (UserId, SenseId) index turns a race with the learner's own add into a conflict.
            _logger.LogWarning(ex, "Group word assignment save failed with a database conflict");
            return Result.Failure(Error.Conflict("GroupWords.Conflict", "A word was added at the same time. Please retry."));
        }
    }
}
