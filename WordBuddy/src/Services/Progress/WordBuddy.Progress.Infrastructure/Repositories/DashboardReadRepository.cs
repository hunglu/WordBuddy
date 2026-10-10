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
            .Select(i => new DashboardSessionRow(i.SessionId, i.IssuedAtUtc, i.PlannedCount, i.ExpiresAtUtc))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<DashboardSessionRow>>(rows);
    }

    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardReviewRow>>>> GetReviewLogsForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await _dbContext.ReviewLogs
            .AsNoTracking()
            .Where(l => userIds.Contains(l.UserId) && l.OccurredAtUtc >= fromUtc && l.OccurredAtUtc < toUtc)
            .Select(l => new
            {
                l.UserId,
                Row = new DashboardReviewRow(
                    l.SenseId, l.SessionId, l.OccurredAtUtc, l.Skill, l.IsCorrect, l.ResponseMs, l.HintUsed, l.IsDue, l.AttemptNo),
            })
            .ToListAsync(ct);
        return Result.Success(Group(rows.Select(r => (r.UserId, r.Row))));
    }

    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardWordRow>>>> GetWordStatesForUsersAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        var rows = await _dbContext.LearnerWordStates
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId) && s.IsActive)
            .Select(s => new { s.UserId, Row = new DashboardWordRow(s.SenseId, s.Status, s.Lapses) })
            .ToListAsync(ct);
        return Result.Success(Group(rows.Select(r => (r.UserId, r.Row))));
    }

    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardMembershipRow>>>> GetMembershipsForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await _dbContext.LearnerWordMemberships
            .AsNoTracking()
            .Where(m => userIds.Contains(m.UserId) && m.IsActive && m.AddedAtUtc >= fromUtc && m.AddedAtUtc < toUtc)
            .Select(m => new { m.UserId, Row = new DashboardMembershipRow(m.SenseId, m.AddedBy, m.AddedAtUtc) })
            .ToListAsync(ct);
        return Result.Success(Group(rows.Select(r => (r.UserId, r.Row))));
    }

    public async Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardSessionRow>>>> GetSessionIssuesForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await _dbContext.VocabularySessionIssues
            .AsNoTracking()
            .Where(i => userIds.Contains(i.UserId) && i.IssuedAtUtc >= fromUtc && i.IssuedAtUtc < toUtc)
            .Select(i => new { i.UserId, Row = new DashboardSessionRow(i.SessionId, i.IssuedAtUtc, i.PlannedCount, i.ExpiresAtUtc) })
            .ToListAsync(ct);
        return Result.Success(Group(rows.Select(r => (r.UserId, r.Row))));
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<T>> Group<T>(IEnumerable<(Guid UserId, T Row)> rows) =>
        rows.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => (IReadOnlyList<T>)g.Select(r => r.Row).ToList());
}
