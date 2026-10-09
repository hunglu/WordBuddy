using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

/// <summary>Read-only dashboard queries: projected columns, <c>AsNoTracking</c>, bounded by time.</summary>
internal sealed class DashboardReadRepository : IDashboardReadRepository
{
    private readonly ProgressDbContext _dbContext;

    public DashboardReadRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<DashboardReviewRow>>> GetReviewLogsAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        List<DashboardReviewRow> rows = await _dbContext.ReviewLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.OccurredAtUtc >= fromUtc && l.OccurredAtUtc < toUtc)
            .Select(l => new DashboardReviewRow(
                l.SenseId, l.SessionId, l.OccurredAtUtc, l.Skill, l.IsCorrect, l.ResponseMs, l.HintUsed, l.IsDue, l.AttemptNo))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<DashboardReviewRow>>(rows);
    }

    public async Task<Result<IReadOnlyList<DashboardWordRow>>> GetWordStatesAsync(Guid userId, CancellationToken ct = default)
    {
        List<DashboardWordRow> rows = await _dbContext.LearnerWordStates
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => new DashboardWordRow(s.SenseId, s.Status, s.Lapses))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<DashboardWordRow>>(rows);
    }

    public async Task<Result<IReadOnlyList<DashboardMembershipRow>>> GetMembershipsAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        List<DashboardMembershipRow> rows = await _dbContext.LearnerWordMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive && m.AddedAtUtc >= fromUtc && m.AddedAtUtc < toUtc)
            .Select(m => new DashboardMembershipRow(m.SenseId, m.AddedBy, m.AddedAtUtc))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<DashboardMembershipRow>>(rows);
    }

    public async Task<Result<IReadOnlyList<DashboardSessionRow>>> GetSessionIssuesAsync(
        Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        List<DashboardSessionRow> rows = await _dbContext.VocabularySessionIssues
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.IssuedAtUtc >= fromUtc && i.IssuedAtUtc < toUtc)
            .Select(i => new DashboardSessionRow(i.SessionId, i.IssuedAtUtc, i.PlannedCount))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<DashboardSessionRow>>(rows);
    }
}
