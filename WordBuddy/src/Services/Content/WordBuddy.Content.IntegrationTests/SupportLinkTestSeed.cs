using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>Seeds <see cref="SupportLinkProjection"/> rows directly (the event path is covered by the consumer tests).</summary>
public static class SupportLinkTestSeed
{
    /// <summary>Gives <paramref name="learnerId"/> one active supporter. Returns the link id.</summary>
    public static async Task<Guid> SeedActiveSupporterAsync(IServiceProvider services, Guid learnerId, Guid? supporterId = null)
    {
        Guid linkId = Guid.NewGuid();
        using IServiceScope scope = services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        dbContext.SupportLinkProjections.Add(
            SupportLinkProjection.Create(linkId, learnerId, supporterId ?? Guid.NewGuid(), isActive: true, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
        return linkId;
    }
}
