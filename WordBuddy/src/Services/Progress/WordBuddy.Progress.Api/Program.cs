using Serilog;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Persistence;
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
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
            .AddWordBuddyServices()
            .AddVocabularySrs(builder.Configuration)
            .AddDashboard(builder.Configuration)
            .AddWordBuddyRateLimiting()
            .AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration);

        builder.Services.AddWordBuddyHealthChecks()
            .AddDbContextCheck<ProgressDbContext>(tags: [HealthCheckExtensions.ReadyTag])
            .AddCheck<ContentHealthCheck>("content", tags: [HealthCheckExtensions.ReadyTag]);

        WebApplication app = builder.Build();

        try
        {
            await app.UseWordBuddyMiddlewareAsync();
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
