using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.Api.Extensions;

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
            ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            logger.LogInformation("Progress migration started");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Progress migration finished");
            return 0;
        }
        catch (Exception exception)
        {
            MigrationFailureLog.Write(logger, "Progress", exception);
            return 1;
        }
    }
}
