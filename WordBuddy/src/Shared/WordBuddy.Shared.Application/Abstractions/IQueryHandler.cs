using WordBuddy.Shared.Kernel;

namespace WordBuddy.Shared.Application.Abstractions;

/// <summary>Handles a <see cref="IQuery{TResult}"/>, producing a <typeparamref name="TResult"/> on success.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The value returned on success.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Handles the query.</summary>
    /// <param name="query">The query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> with the value or an error.</returns>
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}
