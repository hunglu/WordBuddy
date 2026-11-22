using Microsoft.EntityFrameworkCore;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.Api.Extensions;

/// <summary>The <c>migrate</c> run mode: applies migrations, then exits. No sample data.</summary>
public static class MigrationRunner
{
    /// <summary>Command-line argument that selects this mode.</summary>
    public const string Argument = "migrate";

    /// <summary>Applies all pending migrations. Returns the process exit code (0 ok, 1 failed).</summary>
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationRunner");

        try
        {
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            logger.LogInformation("Content migration started");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Content migration finished");
            return 0;
        }
        catch (Exception exception)
        {
            // Only the exception type is logged: provider messages can contain the connection string.
            logger.LogError("Content migration failed: {ExceptionType}", exception.GetType().Name);
            return 1;
        }
    }
}
