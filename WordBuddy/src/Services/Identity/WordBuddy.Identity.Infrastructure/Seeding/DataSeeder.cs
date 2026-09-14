using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Infrastructure.Persistence;

namespace WordBuddy.Identity.Infrastructure.Seeding;

/// <summary>Seeds the Identity database with a development admin account. Invoked only in Development.</summary>
public static class DataSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        await dbContext.Database.MigrateAsync();

        if (await dbContext.Users.AnyAsync())
        {
            logger.LogWarning("Identity seed skipped: Users table already has data");
            return;
        }

        logger.LogInformation("Seeding Identity development data");

        User admin = new(
            Guid.NewGuid(),
            "admin@wordbuddy.com",
            "Admin",
            BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            AgeGroup.Adult,
            isAdmin: true);

        await dbContext.Users.AddAsync(admin);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Identity seed complete: 1 admin user created");
    }
}
