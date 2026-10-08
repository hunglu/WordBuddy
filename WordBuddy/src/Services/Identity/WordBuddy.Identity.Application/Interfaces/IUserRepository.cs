using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>Persists and retrieves <see cref="User"/> accounts.</summary>
public interface IUserRepository
{
    /// <summary>Returns the user with the given email, or <see cref="Error.NotFound"/> if none exists.</summary>
    Task<Result<User>> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if a user with the given email already exists.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Persists a new user.</summary>
    Task<Result> AddAsync(User user, CancellationToken ct = default);

    /// <summary>Returns the user (read-only), or <see cref="UserErrors.NotFound"/>.</summary>
    Task<Result<User>> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns the tracked user, or <see cref="UserErrors.NotFound"/>.</summary>
    Task<Result<User>> GetTrackedByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns the users with the given ids (read-only). Missing ids are skipped.</summary>
    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Whether another user already has <paramref name="alias"/> (case-insensitive).</summary>
    Task<bool> AliasTakenAsync(string alias, Guid exceptUserId, CancellationToken ct = default);

    /// <summary>Saves changes to tracked users.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
