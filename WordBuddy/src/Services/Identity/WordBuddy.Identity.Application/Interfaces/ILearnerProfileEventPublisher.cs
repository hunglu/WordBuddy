using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>
/// Stages <c>LearnerProfileUpdated</c> in the transactional outbox. Call before the save that
/// commits the profile change, so the event and the change commit together.
/// Payload: no email, no password; display name only for adults.
/// </summary>
public interface ILearnerProfileEventPublisher
{
    /// <summary>Stages one profile snapshot of <paramref name="user"/>.</summary>
    Task PublishAsync(User user, DateTime occurredAtUtc, CancellationToken ct = default);
}
