using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Messaging;
using WordBuddy.Shared.Contracts.SupportLinks;

namespace WordBuddy.Progress.IntegrationTests.Messaging;

/// <summary>Support-link events are consumed from Progress-owned queues, never a queue shared with Content.</summary>
[Collection(ProgressApiCollection.Name)]
public sealed class SupportLinkQueueTests
{
    private readonly ProgressApiFactory _factory;

    public SupportLinkQueueTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SupportLinkConsumers_Consume_FromProgressPrefixedQueues()
    {
        ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
        Guid linkId = Guid.NewGuid();

        await harness.Bus.Publish(new SupportLinkActivated(linkId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        await harness.Bus.Publish(new SupportLinkRevoked(linkId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddSeconds(1)));

        // Poll the recorded list instead of Consumed.Any(): Any() gives up as soon as the harness inactivity
        // timer (started with the host, 1.2 s) has fired. A cold start that migrates the database first
        // (slower on PostgreSQL) outlives that timer, so Any() returned false before the consumers ran.
        (await WaitUntilAsync(() => harness.Consumed.Select<SupportLinkActivated>(m => m.Context.Message.LinkId == linkId).Any()))
            .Should().BeTrue("the activated consumer must run");
        (await WaitUntilAsync(() => harness.Consumed.Select<SupportLinkRevoked>(m => m.Context.Message.LinkId == linkId).Any()))
            .Should().BeTrue("the revoked consumer must run");

        // Consumed also lists faulted consumes; the stored projection proves both consumers succeeded.
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        SupportLinkProjection stored = await db.SupportLinkProjections.AsNoTracking().SingleAsync(p => p.LinkId == linkId);
        stored.IsActive.Should().BeFalse("the revoke is the newest event");

        // The revoke alone creates the same row, so also check the activate consume did not fault.
        harness.Consumed.Select<SupportLinkActivated>(m => m.Context.Message.LinkId == linkId)
            .All(m => m.Exception is null).Should().BeTrue("the activated consumer must not fault");
        harness.Consumed.Select<SupportLinkRevoked>(m => m.Context.Message.LinkId == linkId)
            .All(m => m.Exception is null).Should().BeTrue("the revoked consumer must not fault");

        harness.Consumed.Select<SupportLinkActivated>(m => m.Context.Message.LinkId == linkId).First()
            .Context.ReceiveContext.InputAddress!.AbsolutePath.Should().EndWith("/" + SupportLinkQueues.Activated);
        harness.Consumed.Select<SupportLinkRevoked>(m => m.Context.Message.LinkId == linkId).First()
            .Context.ReceiveContext.InputAddress!.AbsolutePath.Should().EndWith("/" + SupportLinkQueues.Revoked);
        SupportLinkQueues.Activated.Should().Be("progress-support-link-activated");
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(50);
        }

        return condition();
    }
}
