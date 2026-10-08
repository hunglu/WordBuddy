using Serilog;
using WordBuddy.Identity.Infrastructure.Seeding;
using WordBuddy.Shared.Infrastructure.Health;

namespace WordBuddy.Identity.Api.Extensions;

internal static class WebApplicationExtensions
{
    /// <summary>Wires the full middleware pipeline and runs the Development seeder.</summary>
    public static async Task UseWordBuddyMiddlewareAsync(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            await DataSeeder.MigrateAndSeedAsync(app.Services);
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapWordBuddyHealthChecks();
        app.MapControllers();
    }
}
