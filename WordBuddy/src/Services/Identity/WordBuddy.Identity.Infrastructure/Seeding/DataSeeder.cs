using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Infrastructure.Persistence;

namespace WordBuddy.Identity.Infrastructure.Seeding;

/// <summary>
/// Seeds the Identity database with development accounts. Invoked only in Development.
/// Each account is added only if its email does not exist yet, so new seed accounts also reach
/// databases that were seeded earlier.
/// </summary>
public static class DataSeeder
{
    private sealed record SeedUser(string Email, string DisplayName, string Password, AgeGroup AgeGroup, bool IsAdmin);

    private static readonly SeedUser[] SeedUsers =
    [
        new("admin@wordbuddy.com", "Admin", "Admin@123", AgeGroup.Adult, IsAdmin: true),
        // Adult learner used by the e2e/ui suite (E2E_TEST_EMAIL / E2E_TEST_PASSWORD defaults).
        new("learner@example.com", "Learner", "ChangeMe123!", AgeGroup.Adult, IsAdmin: false),
    ];

    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        await dbContext.Database.MigrateAsync();

        List<string> existingEmails = await dbContext.Users.Select(user => user.Email).ToListAsync();
        int created = 0;

        foreach (SeedUser seed in SeedUsers.Where(seed => !existingEmails.Contains(seed.Email)))
        {
            User user = new(
                Guid.NewGuid(),
                seed.Email,
                seed.DisplayName,
                BCrypt.Net.BCrypt.HashPassword(seed.Password, workFactor: 12),
                seed.AgeGroup,
                seed.IsAdmin);

            await dbContext.Users.AddAsync(user);
            created++;
        }

        if (created == 0)
        {
            logger.LogInformation("Identity seed skipped: all seed users already exist");
            return;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Identity seed complete: {CreatedCount} user(s) created", created);
    }
}
