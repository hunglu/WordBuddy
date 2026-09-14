using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Abstractions;

/// <summary>Handles a <see cref="IQuery{TResult}"/>, producing a <typeparamref name="TResult"/> on success.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}
