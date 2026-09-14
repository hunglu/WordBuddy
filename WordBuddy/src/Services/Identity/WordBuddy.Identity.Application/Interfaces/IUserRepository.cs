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
}
