using MassTransit;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Contracts.SupportLinks;

namespace WordBuddy.Identity.Infrastructure.Messaging;

/// <summary>Publishes through the scoped EF Core bus outbox: each publish adds an outbox row to
/// <c>IdentityDbContext</c>, committed by the next save together with the link change.</summary>
internal sealed class OutboxSupportLinkEventPublisher : ISupportLinkEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OutboxSupportLinkEventPublisher> _logger;

    public OutboxSupportLinkEventPublisher(IPublishEndpoint publishEndpoint, ILogger<OutboxSupportLinkEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishActivatedAsync(SupportLink link, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(new SupportLinkActivated(link.Id, link.LearnerId, link.SupporterId, occurredAtUtc), ct);
        _logger.LogDebug("Staged SupportLinkActivated: LinkId={LinkId}", link.Id);
    }

    public async Task PublishRevokedAsync(SupportLink link, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(new SupportLinkRevoked(link.Id, link.LearnerId, link.SupporterId, occurredAtUtc), ct);
        _logger.LogDebug("Staged SupportLinkRevoked: LinkId={LinkId}", link.Id);
    }
}
