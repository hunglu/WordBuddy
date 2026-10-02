using Serilog;
using WordBuddy.Shared.Infrastructure.Health;

namespace WordBuddy.Notification.Api.Extensions;

internal static class WebApplicationExtensions
{
    /// <summary>Wires the middleware pipeline. No database to migrate/seed yet — scaffold only.</summary>
    public static Task UseWordBuddyMiddlewareAsync(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapWordBuddyHealthChecks();
        app.MapControllers();

        return Task.CompletedTask;
    }
}
