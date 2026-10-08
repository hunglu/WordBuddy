using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IdentityDbContext dbContext, ILogger<UserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<User>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying User by Email={Email}", email);

        User? user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            _logger.LogWarning("User not found for Email={Email}", email);
            return Result.Failure<User>(Error.NotFound("User.NotFound", $"No user found with email '{email}'."));
        }

        return Result.Success(user);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        _dbContext.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);

    public async Task<Result> AddAsync(User user, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding User with Id={UserId}", user.Id);

        await _dbContext.Users.AddAsync(user, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<User>> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? Result.Failure<User>(UserErrors.NotFound) : Result.Success(user);
    }

    public async Task<Result<User>> GetTrackedByIdAsync(Guid userId, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? Result.Failure<User>(UserErrors.NotFound) : Result.Success(user);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
        await _dbContext.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToListAsync(ct);

    public Task<bool> AliasTakenAsync(string alias, Guid exceptUserId, CancellationToken ct = default) =>
        _dbContext.Users.AsNoTracking().AnyAsync(u => u.Alias == alias && u.Id != exceptUserId, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // The filtered unique alias index catches a race between two equal aliases.
            _logger.LogWarning(ex, "User save failed with a database conflict");
            return Result.Failure(UserErrors.AliasTaken);
        }
    }
}
