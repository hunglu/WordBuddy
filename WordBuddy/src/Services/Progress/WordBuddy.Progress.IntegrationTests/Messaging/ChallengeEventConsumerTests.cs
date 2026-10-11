using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Progress.Infrastructure.Messaging;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Contracts.Profiles;
using WordBuddy.Shared.Contracts.SupportLinks;

namespace WordBuddy.Progress.IntegrationTests.Messaging;

/// <summary>WB-29: Identity events reach the challenge tables through Progress-owned queues.</summary>
[Collection(ProgressApiCollection.Name)]
public sealed class ChallengeEventConsumerTests
{
    private readonly ProgressApiFactory _factory;

    public ChallengeEventConsumerTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<T> WaitForAsync<T>(Func<ProgressDbContext, Task<T?>> read) where T : class
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            T? value = await read(scope.ServiceProvider.GetRequiredService<ProgressDbContext>());
            if (value is not null)
            {
                return value;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("The event was not applied in time.");
    }

    [Fact]
    public async Task LearnerProfileUpdatedConsumer_Consume_UpsertsTheProfileFromAProgressQueueAndKeepsOneRow()
    {
        ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
        Guid userId = Guid.NewGuid();
        DateTime at = DateTime.UtcNow;

        await harness.Bus.Publish(new LearnerProfileUpdated(userId, "Child", "Fox", "fox", "Real Name", "Asia/Ho_Chi_Minh", false, at));
        await harness.Bus.Publish(new LearnerProfileUpdated(userId, "Child", "Fox", "fox", "Real Name", "Asia/Ho_Chi_Minh", false, at));
        await harness.Bus.Publish(new LearnerProfileUpdated(userId, "Child", "Old", "owl", null, null, false, at.AddMinutes(-5)));

        ChallengeProfile profile = await WaitForAsync(async db =>
            await db.ChallengeProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId && p.Alias == "Fox"));
        await Task.Delay(500);

        profile.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
        profile.DisplayName.Should().BeNull("a child display name is never stored");
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext check = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        (await check.ChallengeProfiles.CountAsync(p => p.UserId == userId)).Should().Be(1);
        (await check.ChallengeProfiles.AsNoTracking().SingleAsync(p => p.UserId == userId)).Alias.Should().Be("Fox", "the older event is ignored");
        harness.Consumed.Select<LearnerProfileUpdated>(m => m.Context.Message.UserId == userId).First()
            .Context.ReceiveContext.InputAddress!.AbsolutePath.Should().EndWith("/" + LearnerProfileQueues.Updated);
    }

    [Fact]
    public async Task SupportLinkRevokedConsumer_Consume_DeletesTheChallengeConsentOfThatSupporter()
    {
        ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
        Guid child = Guid.NewGuid();
        Guid guardian = Guid.NewGuid();
        Guid linkId = await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, child, guardian);
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            db.ChallengeConsents.Add(ChallengeConsent.Create(child, guardian, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        await harness.Bus.Publish(new SupportLinkRevoked(linkId, child, guardian, DateTime.UtcNow.AddMinutes(1)));

        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        bool stillThere = true;
        while (DateTime.UtcNow < deadline && stillThere)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            stillThere = await db.ChallengeConsents.AnyAsync(c => c.ChildId == child);
            if (stillThere)
            {
                await Task.Delay(100);
            }
        }

        stillThere.Should().BeFalse("a revoked link ends the consent that supporter gave");
    }
}
