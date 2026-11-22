using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Infrastructure.Persistence;

namespace WordBuddy.Identity.Infrastructure.Seeding;

/// <summary>
/// Creates the first admin account on a fresh Production database. Runs from the <c>migrate</c> mode only.
/// Never logs the email or the password.
/// </summary>
public static class AdminBootstrapper
{
    /// <summary>
    /// Creates one Adult admin when none exists. Returns <c>false</c> when no admin exists and the options are not usable.
    /// </summary>
    /// <param name="dbContext">The Identity database.</param>
    /// <param name="options">The <c>Bootstrap:Admin</c> settings.</param>
    /// <param name="logger">Logger (message templates only).</param>
    public static async Task<bool> RunAsync(IdentityDbContext dbContext, AdminBootstrapOptions options, ILogger logger)
    {
        bool adminExists = await dbContext.Users.AnyAsync(user => user.IsAdmin);
        if (adminExists)
        {
            logger.LogInformation("Admin bootstrap skipped: an admin already exists");
            return true;
        }

        if (!options.IsValid())
        {
            logger.LogError("Admin bootstrap not configured: set Bootstrap:Admin Email and Password (at least {MinLength} characters)", AdminBootstrapOptions.PasswordMinLength);
            return false;
        }

        User admin = new(
            Guid.NewGuid(),
            options.Email!,
            options.DisplayName,
            BCrypt.Net.BCrypt.HashPassword(options.Password!, workFactor: 12),
            AgeGroup.Adult,
            isAdmin: true);

        await dbContext.Users.AddAsync(admin);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Admin bootstrap complete: admin created");
        return true;
    }
}
