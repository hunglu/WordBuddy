using Serilog;
using WordBuddy.Quiz.Api.Extensions;
using WordBuddy.Quiz.Infrastructure.Persistence;
using WordBuddy.Shared.Infrastructure;
using WordBuddy.Shared.Infrastructure.Health;
using WordBuddy.Shared.Infrastructure.Observability;

namespace WordBuddy.Quiz.Api;

/// <summary>Application entry point for the Quiz API host.</summary>
public sealed class Program
{
    private const string ServiceName = "WordBuddy.Quiz";

    /// <summary>Builds, configures, and starts the Quiz API host.</summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.ConfigureWordBuddySerilog(ServiceName);

        builder.Services
            .AddWordBuddyForwardedHeaders(builder.Configuration)
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
            .AddWordBuddyServices()
            .AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration);

        builder.Services.AddWordBuddyHealthChecks()
            .AddDbContextCheck<QuizDbContext>(tags: [HealthCheckExtensions.ReadyTag]);

        WebApplication app = builder.Build();

        try
        {
            // "migrate" run mode: apply migrations, then exit (no web server). Used by one-shot jobs and init containers.
            if (args.Length > 0 && string.Equals(args[0], MigrationRunner.Argument, StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = await MigrationRunner.RunAsync(app.Services);
                return;
            }

            await app.UseWordBuddyMiddlewareAsync();
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
