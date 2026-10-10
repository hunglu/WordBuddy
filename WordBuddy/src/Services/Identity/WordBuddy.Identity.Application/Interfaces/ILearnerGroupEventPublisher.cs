using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>
/// Stages group events in the transactional outbox. Call before
/// <see cref="ILearnerGroupRepository.SaveChangesAsync"/>, so the event and the change commit
/// together. Payload: ids, reason code and time only.
/// </summary>
public interface ILearnerGroupEventPublisher
{
    /// <summary>Stages <c>LearnerGroupMemberActivated</c>.</summary>
    Task PublishMemberActivatedAsync(Guid groupId, Guid ownerId, Guid learnerId, DateTime occurredAtUtc, CancellationToken ct = default);

    /// <summary>Stages <c>LearnerGroupMemberRemoved</c>.</summary>
    Task PublishMemberRemovedAsync(
        Guid groupId, Guid ownerId, Guid learnerId, GroupMemberRemovedReason reason, DateTime occurredAtUtc, CancellationToken ct = default);

    /// <summary>Stages <c>LearnerGroupDeleted</c>.</summary>
    Task PublishGroupDeletedAsync(Guid groupId, Guid ownerId, DateTime occurredAtUtc, CancellationToken ct = default);
}
