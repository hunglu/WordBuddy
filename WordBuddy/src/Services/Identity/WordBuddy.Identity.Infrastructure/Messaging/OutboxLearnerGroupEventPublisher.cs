using MassTransit;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Contracts.Groups;

namespace WordBuddy.Identity.Infrastructure.Messaging;

/// <summary>Publishes through the scoped EF Core bus outbox: each publish adds an outbox row to
/// <c>IdentityDbContext</c>, committed by the next save together with the group change.</summary>
internal sealed class OutboxLearnerGroupEventPublisher : ILearnerGroupEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OutboxLearnerGroupEventPublisher> _logger;

    public OutboxLearnerGroupEventPublisher(IPublishEndpoint publishEndpoint, ILogger<OutboxLearnerGroupEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishMemberActivatedAsync(Guid groupId, Guid ownerId, Guid learnerId, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(new LearnerGroupMemberActivated(groupId, ownerId, learnerId, occurredAtUtc), ct);
        _logger.LogDebug("Staged LearnerGroupMemberActivated: GroupId={GroupId}", groupId);
    }

    public async Task PublishMemberRemovedAsync(
        Guid groupId, Guid ownerId, Guid learnerId, GroupMemberRemovedReason reason, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(new LearnerGroupMemberRemoved(groupId, ownerId, learnerId, reason.ToString(), occurredAtUtc), ct);
        _logger.LogDebug("Staged LearnerGroupMemberRemoved: GroupId={GroupId}, Reason={Reason}", groupId, reason);
    }

    public async Task PublishGroupDeletedAsync(Guid groupId, Guid ownerId, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(new LearnerGroupDeleted(groupId, ownerId, occurredAtUtc), ct);
        _logger.LogDebug("Staged LearnerGroupDeleted: GroupId={GroupId}", groupId);
    }
}
