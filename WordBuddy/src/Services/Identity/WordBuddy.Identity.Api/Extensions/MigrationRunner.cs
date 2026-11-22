using Microsoft.EntityFrameworkCore;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Identity.Infrastructure.Seeding;

namespace WordBuddy.Identity.Api.Extensions;

/// <summary>The <c>migrate</c> run mode: applies migrations, creates the first admin, then exits.</summary>
public static class MigrationRunner
{
    /// <summary>Command-line argument that selects this mode.</summary>
    public const string Argument = "migrate";

    /// <summary>Runs the migration and admin bootstrap. Returns the process exit code (0 ok, 1 failed).</summary>
    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        using IServiceScope scope = services.CreateScope();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationRunner");

        try
        {
            IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            logger.LogInformation("Identity migration started");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Identity migration finished");

            AdminBootstrapOptions options = configuration.GetSection(AdminBootstrapOptions.SectionName).Get<AdminBootstrapOptions>() ?? new();
            return await AdminBootstrapper.RunAsync(dbContext, options, logger) ? 0 : 1;
        }
        catch (Exception exception)
        {
            // Only the exception type is logged: provider messages can contain the connection string.
            logger.LogError("Identity migration failed: {ExceptionType}", exception.GetType().Name);
            return 1;
        }
    }
}
