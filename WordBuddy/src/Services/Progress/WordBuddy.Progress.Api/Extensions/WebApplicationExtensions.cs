using Microsoft.EntityFrameworkCore;
using Serilog;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.Api.Extensions;

internal static class WebApplicationExtensions
{
    /// <summary>Wires the full middleware pipeline and applies migrations in Development (no seed data — Progress starts empty per user).</summary>
    public static async Task UseWordBuddyMiddlewareAsync(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            using IServiceScope scope = app.Services.CreateScope();
            ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
