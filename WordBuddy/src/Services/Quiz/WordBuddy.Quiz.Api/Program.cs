using Serilog;
using WordBuddy.Quiz.Api.Extensions;
using WordBuddy.Quiz.Infrastructure.Persistence;
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
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
            .AddWordBuddyServices()
            .AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration);

        builder.Services.AddWordBuddyHealthChecks()
            .AddDbContextCheck<QuizDbContext>(tags: [HealthCheckExtensions.ReadyTag]);

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
