using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Contracts.Vocabulary;

namespace WordBuddy.Content.IntegrationTests.Messaging;

/// <summary>
/// Every <see cref="LearnerWord"/> insert/delete goes out through the EF Core outbox. Runs the real
/// API on SQL Server with the in-memory transport (<c>Messaging:Transport = InMemory</c>) and
/// records what a subscriber receives through a temporary receive endpoint on the in-memory bus
/// (the outbox delivers a serialized copy, so a real subscriber is the faithful check). Each test
/// uses fresh user ids.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class LearnerWordEventPublishingTests : IAsyncLifetime
{
    private readonly ContentApiFactory _factory;
    private readonly ConcurrentBag<LearnerWordAdded> _added = [];
    private readonly ConcurrentBag<LearnerWordRemoved> _removed = [];
    private HostReceiveEndpointHandle? _endpoint;

    public LearnerWordEventPublishingTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        IBus bus = _factory.Services.GetRequiredService<IBus>();
        _endpoint = bus.ConnectReceiveEndpoint($"content-tests-{Guid.NewGuid():N}", endpoint =>
        {
            endpoint.Handler<LearnerWordAdded>(context =>
            {
                _added.Add(context.Message);
                return Task.CompletedTask;
            });
            endpoint.Handler<LearnerWordRemoved>(context =>
            {
                _removed.Add(context.Message);
                return Task.CompletedTask;
            });
        });
        await _endpoint.Ready;
    }

    public async Task DisposeAsync()
    {
        if (_endpoint is not null)
        {
            await _endpoint.StopAsync();
        }
    }

    private HttpClient CreateClient(Guid userId, string ageGroup, bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    private static async Task<Guid> AddWordAsync(HttpClient client, string word)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest(word, "a definition", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> AddSharedWordAsync(HttpClient owner, string word)
    {
        Guid id = await AddWordAsync(owner, word);
        HttpClient admin = CreateClient(Guid.NewGuid(), "Adult", isAdmin: true);
        (await owner.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(true, true)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return id;
    }

    /// <summary>The outbox delivers after its query delay, so poll instead of waiting for harness inactivity.</summary>
    private static async Task<bool> EventuallyAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(100);
        }

        return condition();
    }

    private Task<bool> AddedPublishedAsync(Guid userId, Guid senseId, int timeoutSeconds = 15) =>
        EventuallyAsync(
            () => _added.Any(m => m.UserId == userId && m.SenseId == senseId),
            TimeSpan.FromSeconds(timeoutSeconds));

    private Task<bool> RemovedPublishedAsync(Guid userId, Guid senseId) =>
        EventuallyAsync(
            () => _removed.Any(m => m.UserId == userId && m.SenseId == senseId),
            TimeSpan.FromSeconds(15));

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task AddWord_PublishesLearnerWordAdded_SamePayloadForChildAndAdult(string ageGroup)
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = await AddWordAsync(CreateClient(userId, ageGroup), $"event-add-{ageGroup}-{userId:N}");

        (await AddedPublishedAsync(userId, senseId)).Should().BeTrue();

        LearnerWordAdded published = _added.First(m => m.UserId == userId);
        published.AddedBy.Should().Be(userId);
        published.SenseId.Should().Be(senseId);
    }

    [Fact]
    public async Task AdoptSharedWord_PublishesLearnerWordAddedForAdopter()
    {
        HttpClient owner = CreateClient(Guid.NewGuid(), "Adult");
        Guid senseId = await AddSharedWordAsync(owner, $"event-adopt-{Guid.NewGuid():N}");
        Guid adopterId = Guid.NewGuid();

        (await CreateClient(adopterId, "Adult").PostAsync($"/api/vocabulary/shared/{senseId}/add-to-mine", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        (await AddedPublishedAsync(adopterId, senseId)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteOwnPrivateWord_OrphanDelete_PublishesLearnerWordRemoved()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = CreateClient(userId, "Adult");
        Guid senseId = await AddWordAsync(client, $"event-orphan-{userId:N}");

        (await client.DeleteAsync($"/api/vocabulary/{senseId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RemovedPublishedAsync(userId, senseId)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSharedWordConfirmed_HandOver_PublishesRemovedForOwnerOnly()
    {
        Guid ownerId = Guid.NewGuid();
        HttpClient owner = CreateClient(ownerId, "Adult");
        Guid senseId = await AddSharedWordAsync(owner, $"event-handover-{ownerId:N}");

        (await owner.DeleteAsync($"/api/vocabulary/{senseId}?confirm=true")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RemovedPublishedAsync(ownerId, senseId)).Should().BeTrue();
        _added.Should().NotContain(m => m.UserId == SystemOwner.UserId);
        _removed.Should().NotContain(m => m.UserId == SystemOwner.UserId);
    }

    [Fact]
    public async Task SaveChangesRolledBack_WritesNoOutboxRowAndPublishesNothing()
    {
        Guid ownerId = Guid.NewGuid();
        Guid senseId = await AddWordAsync(CreateClient(ownerId, "Adult"), $"event-rollback-{ownerId:N}");
        Guid otherUserId = Guid.NewGuid();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.LearnerWords.AddAsync(new LearnerWord(Guid.NewGuid(), otherUserId, senseId, isAuthor: false));
            await dbContext.SaveChangesAsync();

            int outboxRowsInTransaction = await dbContext.Set<MassTransit.EntityFrameworkCoreIntegration.OutboxMessage>().CountAsync();
            outboxRowsInTransaction.Should().BeGreaterThan(0);

            await transaction.RollbackAsync();
        }

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            (await dbContext.LearnerWords.AnyAsync(l => l.UserId == otherUserId)).Should().BeFalse();
        }

        (await AddedPublishedAsync(otherUserId, senseId, timeoutSeconds: 3)).Should().BeFalse();
    }

    [Fact]
    public async Task RepublishLearnerWords_Admin_PublishesAddedPerLinkWithOriginalTime()
    {
        Guid userId = Guid.NewGuid();
        HttpClient learner = CreateClient(userId, "Child");
        Guid firstSenseId = await AddWordAsync(learner, $"event-backfill-a-{userId:N}");
        Guid secondSenseId = await AddWordAsync(learner, $"event-backfill-b-{userId:N}");
        (await AddedPublishedAsync(userId, firstSenseId)).Should().BeTrue();
        (await AddedPublishedAsync(userId, secondSenseId)).Should().BeTrue();

        Dictionary<Guid, DateTime> addedAt;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            addedAt = await dbContext.LearnerWords.AsNoTracking()
                .Where(l => l.UserId == userId)
                .ToDictionaryAsync(l => l.SenseId, l => l.AddedAtUtc);
        }

        HttpResponseMessage response = await CreateClient(Guid.NewGuid(), "Adult", isAdmin: true)
            .PostAsync("/api/vocabulary/admin/learner-words/republish", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RepublishLearnerWordsResult? result = await response.Content.ReadFromJsonAsync<RepublishLearnerWordsResult>();
        result!.Published.Should().BeGreaterThanOrEqualTo(2);

        (await EventuallyAsync(
            () => _added.Count(m => m.UserId == userId) >= 4,
            TimeSpan.FromSeconds(15))).Should().BeTrue("each link is published once by the add and once by the backfill");

        _added.Where(m => m.UserId == userId).Should().OnlyContain(m => m.AddedAtUtc == addedAt[m.SenseId] && m.AddedBy == userId);
        _added.Should().NotContain(m => m.UserId == SystemOwner.UserId);
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task RepublishLearnerWords_NonAdmin_ReturnsForbidden(string ageGroup)
    {
        HttpResponseMessage response = await CreateClient(Guid.NewGuid(), ageGroup)
            .PostAsync("/api/vocabulary/admin/learner-words/republish", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
