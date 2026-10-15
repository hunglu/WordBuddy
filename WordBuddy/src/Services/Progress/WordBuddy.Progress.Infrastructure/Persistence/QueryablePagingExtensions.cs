using Microsoft.EntityFrameworkCore;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Persistence;

/// <summary>Paging helpers for EF Core queries.</summary>
internal static class QueryablePagingExtensions
{
    /// <summary>
    /// Counts the rows, then reads one page. The caller must order the query first (end the order
    /// with the entity id so pages are stable).
    /// </summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <param name="source">Filtered and ordered query.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The requested page with the real total.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        int total = await source.CountAsync(ct);
        List<T> items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }
}
