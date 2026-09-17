using Serilog;
using WordBuddy.Notification.Api.Extensions;
using WordBuddy.Shared.Infrastructure.Observability;

namespace WordBuddy.Notification.Api;

/// <summary>
/// Application entry point for the Notification API host. Scaffold only as of Phase 2 — no
/// domain entities, database, or controllers yet (see root CLAUDE.md's Notification
/// responsibility: push/email notifications, to be built once there's a message bus event to
/// react to). Still gets the same Serilog/OpenTelemetry/JWT wiring every service gets, so it's
/// ready to grow into real endpoints without re-deriving the setup.
/// </summary>
public sealed class Program
{
    private const string ServiceName = "WordBuddy.Notification";

    /// <summary>Builds, configures, and starts the Notification API host.</summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.ConfigureWordBuddySerilog(ServiceName);

        builder.Services
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyServices()
            .AddWordBuddyOpenTelemetry(ServiceName, builder.Configuration);

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
