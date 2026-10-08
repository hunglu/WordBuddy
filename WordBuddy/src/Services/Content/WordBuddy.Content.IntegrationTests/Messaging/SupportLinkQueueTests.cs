using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Infrastructure.Messaging;
using WordBuddy.Shared.Contracts.SupportLinks;

namespace WordBuddy.Content.IntegrationTests.Messaging;

/// <summary>Support-link events are consumed from Content-owned queues, never a queue shared with Progress.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class SupportLinkQueueTests
{
    private readonly ContentApiFactory _factory;

    public SupportLinkQueueTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SupportLinkConsumers_Consume_FromContentPrefixedQueues()
    {
        ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
        Guid linkId = Guid.NewGuid();

        await harness.Bus.Publish(new SupportLinkActivated(linkId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        await harness.Bus.Publish(new SupportLinkRevoked(linkId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddSeconds(1)));

        (await harness.Consumed.Any<SupportLinkActivated>(m => m.Context.Message.LinkId == linkId)).Should().BeTrue();
        (await harness.Consumed.Any<SupportLinkRevoked>(m => m.Context.Message.LinkId == linkId)).Should().BeTrue();

        harness.Consumed.Select<SupportLinkActivated>(m => m.Context.Message.LinkId == linkId).First()
            .Context.ReceiveContext.InputAddress!.AbsolutePath.Should().EndWith("/" + SupportLinkQueues.Activated);
        harness.Consumed.Select<SupportLinkRevoked>(m => m.Context.Message.LinkId == linkId).First()
            .Context.ReceiveContext.InputAddress!.AbsolutePath.Should().EndWith("/" + SupportLinkQueues.Revoked);
        SupportLinkQueues.Activated.Should().Be("content-support-link-activated");
    }
}
