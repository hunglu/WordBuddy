using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

/// <summary>No delete method exists; <see cref="VocabularySessionIssue.End"/> is the only update.</summary>
internal sealed class VocabularySessionIssueRepository : IVocabularySessionIssueRepository
{
    private readonly ProgressDbContext _dbContext;

    public VocabularySessionIssueRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddAsync(VocabularySessionIssue issue, CancellationToken ct = default)
    {
        await _dbContext.VocabularySessionIssues.AddAsync(issue, ct);
        return Result.Success();
    }

    public async Task<Result<VocabularySessionIssue>> GetOpenAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
    {
        VocabularySessionIssue? issue = await _dbContext.VocabularySessionIssues
            .AsTracking()
            .Include(i => i.Items)
            .Where(i => i.UserId == userId && i.EndedAtUtc == null && i.ExpiresAtUtc > nowUtc)
            .OrderByDescending(i => i.IssuedAtUtc)
            .FirstOrDefaultAsync(ct);

        return issue is null
            ? Result.Failure<VocabularySessionIssue>(Error.NotFound("VocabularySession.NoOpenSession", "No open session."))
            : Result.Success(issue);
    }

    public async Task<Result<IReadOnlySet<Guid>>> GetAnsweredSenseIdsAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        List<Guid> ids = await _dbContext.ReviewLogs
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.SessionId == sessionId)
            .Select(r => r.SenseId)
            .Distinct()
            .ToListAsync(ct);
        return Result.Success<IReadOnlySet<Guid>>(ids.ToHashSet());
    }
}
