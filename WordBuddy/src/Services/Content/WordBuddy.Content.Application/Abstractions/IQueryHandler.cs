using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Abstractions;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}
