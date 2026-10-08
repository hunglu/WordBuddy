using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>
/// Stages link events in the transactional outbox. Call before
/// <see cref="ISupportLinkRepository.SaveChangesAsync"/>, so the event and the link change commit
/// together. Payload: ids and time only.
/// </summary>
public interface ISupportLinkEventPublisher
{
    /// <summary>Stages <c>SupportLinkActivated</c>.</summary>
    Task PublishActivatedAsync(SupportLink link, DateTime occurredAtUtc, CancellationToken ct = default);

    /// <summary>Stages <c>SupportLinkRevoked</c>.</summary>
    Task PublishRevokedAsync(SupportLink link, DateTime occurredAtUtc, CancellationToken ct = default);
}
