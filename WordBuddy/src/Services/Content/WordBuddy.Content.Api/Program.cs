using Serilog;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Shared.Infrastructure.Observability;

namespace WordBuddy.Content.Api;

/// <summary>Application entry point for the Content API host.</summary>
public sealed class Program
{
    private const string ServiceName = "WordBuddy.Content";

    /// <summary>Builds, configures, and starts the Content API host.</summary>
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
