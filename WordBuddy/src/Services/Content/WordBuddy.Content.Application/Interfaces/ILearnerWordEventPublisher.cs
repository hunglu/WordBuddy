using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Publishes learner-word events outside the normal link insert/delete path (backfill).</summary>
public interface ILearnerWordEventPublisher
{
    /// <summary>Publishes one <c>LearnerWordAdded</c> per link through the outbox and saves once.
    /// Returns the number of events published.</summary>
    Task<Result<int>> PublishAddedAsync(IReadOnlyList<LearnerWordLink> links, CancellationToken ct = default);
}

/// <summary>Ids and timestamp of one learner-word link — the payload of a backfill event.</summary>
/// <param name="LinkId">The link row id — the keyset cursor for backfill paging.</param>
/// <param name="UserId">The learner.</param>
/// <param name="SenseId">The linked sense.</param>
/// <param name="AddedAtUtc">Original add time (UTC).</param>
public sealed record LearnerWordLink(Guid LinkId, Guid UserId, Guid SenseId, DateTime AddedAtUtc);
