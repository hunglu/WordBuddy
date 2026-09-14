using WordBuddy.Identity.Api.Extensions;

namespace WordBuddy.Identity.Api;

/// <summary>Application entry point for the Identity API host.</summary>
public sealed class Program
{
    /// <summary>Builds, configures, and starts the Identity API host.</summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddWordBuddyAuthentication(builder.Configuration)
            .AddWordBuddyDatabase(builder.Configuration)
            .AddWordBuddyServices();

        WebApplication app = builder.Build();

        await app.UseWordBuddyMiddlewareAsync();

        app.Run();
    }
}
