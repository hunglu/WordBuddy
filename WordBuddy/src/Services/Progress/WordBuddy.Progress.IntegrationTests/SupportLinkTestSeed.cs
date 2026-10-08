using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>Seeds <see cref="SupportLinkProjection"/> rows directly (the event path is covered by the consumer tests).</summary>
public static class SupportLinkTestSeed
{
    /// <summary>Gives <paramref name="learnerId"/> one active supporter. Returns the link id.</summary>
    public static async Task<Guid> SeedActiveSupporterAsync(IServiceProvider services, Guid learnerId, Guid? supporterId = null)
    {
        Guid linkId = Guid.NewGuid();
        using IServiceScope scope = services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        dbContext.SupportLinkProjections.Add(
            SupportLinkProjection.Create(linkId, learnerId, supporterId ?? Guid.NewGuid(), isActive: true, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
        return linkId;
    }
}
