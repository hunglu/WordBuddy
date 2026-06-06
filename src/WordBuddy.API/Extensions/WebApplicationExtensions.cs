using Serilog;
using WordBuddy.Infrastructure.Seeding;

namespace WordBuddy.API.Extensions;

internal static class WebApplicationExtensions
{
    /// <summary>
    /// Wires Serilog into the ASP.NET Core host logging pipeline.
    /// Configuration is read from the <c>Serilog</c> section in appsettings.
    /// </summary>
    internal static WebApplicationBuilder ConfigureWordBuddyLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, config) =>
            config.ReadFrom.Configuration(context.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext());

        return builder;
    }

    /// <summary>
    /// Configures the HTTP pipeline and, in Development, applies pending EF Core migrations
    /// and seeds sample data before the host begins accepting requests.
    /// </summary>
    internal static async Task UseWordBuddyMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            using IServiceScope scope = app.Services.CreateScope();
            await DataSeeder.MigrateAndSeedAsync(scope.ServiceProvider);
        }
    }
}
