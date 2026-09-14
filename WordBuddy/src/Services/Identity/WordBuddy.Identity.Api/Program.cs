using Serilog;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Shared.Infrastructure.Observability;

namespace WordBuddy.Identity.Api;

/// <summary>Application entry point for the Identity API host.</summary>
public sealed class Program
{
    private const string ServiceName = "WordBuddy.Identity";

    /// <summary>Builds, configures, and starts the Identity API host.</summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.ConfigureWordBuddySerilog(ServiceName);

        builder.Services
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
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
