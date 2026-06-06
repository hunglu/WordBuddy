using WordBuddy.API.Extensions;

namespace WordBuddy.API;

/// <summary>Application entry point for the WordBuddy API host.</summary>
public sealed class Program
{
    /// <summary>Builds, configures, and starts the WordBuddy API host.</summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.ConfigureWordBuddyLogging();

        builder.Services
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
            .AddWordBuddyServices();

        WebApplication app = builder.Build();

        await app.UseWordBuddyMiddleware();

        app.Run();
    }
}
