using MassTransit;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Contracts.Profiles;

namespace WordBuddy.Identity.Infrastructure.Messaging;

/// <summary>Publishes through the scoped EF Core bus outbox: the outbox row is committed by the next save.</summary>
internal sealed class OutboxLearnerProfileEventPublisher : ILearnerProfileEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OutboxLearnerProfileEventPublisher> _logger;

    public OutboxLearnerProfileEventPublisher(IPublishEndpoint publishEndpoint, ILogger<OutboxLearnerProfileEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync(User user, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        // A child's display name is a real name: it never leaves Identity.
        string? displayName = user.AgeGroup == AgeGroup.Child ? null : user.DisplayName;

        await _publishEndpoint.Publish(
            new LearnerProfileUpdated(
                user.Id, user.AgeGroup.ToString(), user.Alias, user.AvatarId, displayName, user.TimeZoneId, user.IsGuardian, occurredAtUtc),
            ct);
        _logger.LogDebug("Staged LearnerProfileUpdated: UserId={UserId}", user.Id);
    }
}
