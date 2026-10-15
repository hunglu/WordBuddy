namespace WordBuddy.Shared.Kernel;

/// <summary>
/// Shared paging constants and an in-memory slice helper.
/// </summary>
public static class Paging
{
    /// <summary>Page size used when the caller sends none.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>Largest page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Takes one page out of an already ordered, in-memory list.
    /// A page past the end returns no items and the real total.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="source">Ordered source list.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <returns>The requested page.</returns>
    public static PagedResult<T> Slice<T>(IReadOnlyList<T> source, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        long skip = ((long)page - 1) * pageSize;
        if (page < 1 || pageSize < 1 || skip >= source.Count)
        {
            return new PagedResult<T>(Array.Empty<T>(), page, pageSize, source.Count);
        }

        int count = (int)Math.Min(pageSize, source.Count - skip);
        T[] items = new T[count];
        for (int i = 0; i < count; i++)
        {
            items[i] = source[(int)skip + i];
        }

        return new PagedResult<T>(items, page, pageSize, source.Count);
    }
}
