using MassTransit;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Contracts.Vocabulary;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Messaging;

/// <summary>Publishes through the scoped EF Core bus outbox: each publish adds an outbox row to
/// <see cref="ContentDbContext"/>, and one save per batch commits them.</summary>
internal sealed class OutboxLearnerWordEventPublisher : ILearnerWordEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<OutboxLearnerWordEventPublisher> _logger;

    public OutboxLearnerWordEventPublisher(
        IPublishEndpoint publishEndpoint,
        ContentDbContext dbContext,
        ILogger<OutboxLearnerWordEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<int>> PublishAddedAsync(IReadOnlyList<LearnerWordLink> links, CancellationToken ct = default)
    {
        foreach (LearnerWordLink link in links)
        {
            await _publishEndpoint.Publish(new LearnerWordAdded(link.UserId, link.SenseId, link.UserId, link.AddedAtUtc), ct);
        }

        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();

        _logger.LogDebug("Published LearnerWordAdded batch: Count={Count}", links.Count);
        return Result.Success(links.Count);
    }
}
