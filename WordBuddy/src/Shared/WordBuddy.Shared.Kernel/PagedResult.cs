namespace WordBuddy.Shared.Kernel;

/// <summary>
/// One page of a larger list, with the numbers a pager needs.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Items">Items on this page.</param>
/// <param name="Page">1-based page number that was served.</param>
/// <param name="PageSize">Maximum items per page.</param>
/// <param name="TotalCount">Number of items across all pages (after filters).</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
