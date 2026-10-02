using Serilog;
using WordBuddy.Quiz.Infrastructure.Seeding;
using WordBuddy.Shared.Infrastructure.Health;

namespace WordBuddy.Quiz.Api.Extensions;

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
        app.MapWordBuddyHealthChecks();
        app.MapControllers();
    }
}
