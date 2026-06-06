using WordBuddy.Domain.Common;

namespace WordBuddy.Application.Interfaces;

/// <summary>Generic repository contract providing basic CRUD operations for a domain entity.</summary>
/// <typeparam name="T">The domain entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Retrieves an entity by its unique identifier. Returns <see cref="Error.NotFound"/> when absent.</summary>
    Task<Result<T>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Retrieves all entities of this type.</summary>
    Task<Result<IReadOnlyList<T>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Persists a new entity.</summary>
    Task<Result> AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Persists changes to an existing entity.</summary>
    Task<Result> UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>Removes the entity with the specified identifier.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
