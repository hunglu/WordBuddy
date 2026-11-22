using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Infrastructure;
using WordBuddy.Shared.Infrastructure.Health;
using WordBuddy.Shared.Infrastructure.Observability;

namespace WordBuddy.Progress.Api;

/// <summary>Application entry point for the Progress API host.</summary>
public sealed class Program
{
    private const string ServiceName = "WordBuddy.Progress";

    /// <summary>Builds, configures, and starts the Progress API host.</summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.ConfigureWordBuddySerilog(ServiceName);

        builder.Services
            .AddWordBuddyForwardedHeaders(builder.Configuration)
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
            .AddWordBuddyServices()
            .AddVocabularySrs(builder.Configuration)
            .AddDashboard(builder.Configuration)
            .AddChallenges(builder.Configuration)
            .AddWordBuddyRateLimiting()
            .AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration);

        IHealthChecksBuilder healthChecks = builder.Services.AddWordBuddyHealthChecks()
            .AddDbContextCheck<ProgressDbContext>(tags: [HealthCheckExtensions.ReadyTag])
            .AddCheck<ContentHealthCheck>("content", failureStatus: HealthStatus.Degraded, tags: [HealthCheckExtensions.ReadyTag]);

        // The gRPC channel is the default; its own readiness check exists only when it is the one in use.
        if (ContentOptions.FromConfiguration(builder.Configuration).Transport == ContentTransport.Grpc)
        {
            healthChecks.AddCheck<ContentGrpcHealthCheck>("content-grpc", failureStatus: HealthStatus.Degraded, tags: [HealthCheckExtensions.ReadyTag]);
        }

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
