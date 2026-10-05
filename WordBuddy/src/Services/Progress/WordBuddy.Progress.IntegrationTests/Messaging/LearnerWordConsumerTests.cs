using System.Net;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Contracts.Vocabulary;
using WordBuddy.Shared.Infrastructure.Health;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Progress.IntegrationTests.Messaging;

/// <summary>
/// Real Progress host on SQL Server with the in-memory transport and the MassTransit test harness.
/// Covers the consumers, the EF inbox (<c>MessageId</c> dedupe) and the readiness check.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class LearnerWordConsumerTests
{
    private static readonly DateTime T0 = new(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly ProgressApiFactory _factory;

    public LearnerWordConsumerTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private ITestHarness Harness => _factory.Services.GetRequiredService<ITestHarness>();

    private Task PublishAsync<T>(T message, Guid messageId)
        where T : class =>
        Harness.Bus.Publish(message, context => context.MessageId = messageId);

    private async Task<LearnerWordMembership?> FindAsync(Guid userId, Guid senseId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await dbContext.LearnerWordMemberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.SenseId == senseId);
    }

    private async Task<int> CountAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await dbContext.LearnerWordMemberships.CountAsync(m => m.UserId == userId);
    }

    private async Task<LearnerWordMembership?> WaitForAsync(Guid userId, Guid senseId, Func<LearnerWordMembership, bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            LearnerWordMembership? membership = await FindAsync(userId, senseId);
            if (membership is not null && condition(membership))
            {
                return membership;
            }

            await Task.Delay(100);
        }

        return await FindAsync(userId, senseId);
    }

    [Fact]
    public async Task LearnerWordAddedConsumer_Consume_CreatesActiveMembership()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();

        await PublishAsync(new LearnerWordAdded(userId, senseId, userId, T0), Guid.NewGuid());

        LearnerWordMembership? membership = await WaitForAsync(userId, senseId, m => m.IsActive);
        membership.Should().NotBeNull();
        membership!.IsActive.Should().BeTrue();
        membership.AddedBy.Should().Be(userId);
    }

    [Fact]
    public async Task LearnerWordAddedConsumer_SameMessageIdTwice_OneRowAndSecondCopyDropped()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();

        await PublishAsync(new LearnerWordAdded(userId, senseId, userId, T0), messageId);
        (await WaitForAsync(userId, senseId, m => m.IsActive)).Should().NotBeNull();

        await PublishAsync(new LearnerWordRemoved(userId, senseId, T0.AddMinutes(1)), Guid.NewGuid());
        (await WaitForAsync(userId, senseId, m => !m.IsActive)).Should().NotBeNull();

        // Redelivery of the first message id, with a newer time: only the inbox can stop it.
        await PublishAsync(new LearnerWordAdded(userId, senseId, userId, T0.AddMinutes(2)), messageId);
        await Task.Delay(TimeSpan.FromSeconds(3));

        (await CountAsync(userId)).Should().Be(1);
        LearnerWordMembership? membership = await FindAsync(userId, senseId);
        membership!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task LearnerWordRemovedConsumer_Consume_MarksMembershipInactive()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();

        await PublishAsync(new LearnerWordAdded(userId, senseId, userId, T0), Guid.NewGuid());
        (await WaitForAsync(userId, senseId, m => m.IsActive)).Should().NotBeNull();

        await PublishAsync(new LearnerWordRemoved(userId, senseId, T0.AddMinutes(1)), Guid.NewGuid());

        LearnerWordMembership? membership = await WaitForAsync(userId, senseId, m => !m.IsActive);
        membership.Should().NotBeNull();
        membership!.IsActive.Should().BeFalse();
        (await CountAsync(userId)).Should().Be(1);
    }

    [Fact]
    public void HealthChecks_Registration_IncludesBrokerCheckTaggedReady()
    {
        HealthCheckServiceOptions options = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.Should().Contain(r =>
            r.Name == MessagingExtensions.HealthCheckName && r.Tags.Contains(HealthCheckExtensions.ReadyTag));
    }

    [Fact]
    public async Task HealthReady_WithBrokerRunning_ReturnsOk()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync(HealthCheckExtensions.ReadinessPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
