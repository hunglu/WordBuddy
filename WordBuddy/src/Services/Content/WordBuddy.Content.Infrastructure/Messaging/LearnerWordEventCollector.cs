using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Contracts.Vocabulary;

namespace WordBuddy.Content.Infrastructure.Messaging;

/// <summary>
/// Maps pending <see cref="LearnerWord"/> inserts and deletes to the
/// <see cref="LearnerWordAdded"/> / <see cref="LearnerWordRemoved"/> contracts. Links of the
/// <see cref="SystemOwner"/> never publish (not a learner). Payload: ids and timestamps only.
/// </summary>
public static class LearnerWordEventCollector
{
    /// <summary>Collects events from the tracked <see cref="LearnerWord"/> entries.</summary>
    /// <param name="changeTracker">The context's change tracker.</param>
    /// <param name="nowUtc">Timestamp used for removals.</param>
    public static IReadOnlyList<object> Collect(ChangeTracker changeTracker, DateTime nowUtc)
    {
        List<(EntityState State, LearnerWord Link)> changes = changeTracker.Entries<LearnerWord>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted)
            .Select(e => (e.State, e.Entity))
            .ToList();

        return Collect(changes, nowUtc);
    }

    /// <summary>Maps (state, link) pairs to events. Other states are ignored.</summary>
    /// <param name="changes">Pending link changes.</param>
    /// <param name="nowUtc">Timestamp used for removals.</param>
    public static IReadOnlyList<object> Collect(IEnumerable<(EntityState State, LearnerWord Link)> changes, DateTime nowUtc)
    {
        List<object> events = [];

        foreach ((EntityState state, LearnerWord link) in changes)
        {
            if (link.UserId == SystemOwner.UserId)
            {
                continue;
            }

            if (state == EntityState.Added)
            {
                events.Add(new LearnerWordAdded(link.UserId, link.SenseId, link.UserId, link.AddedAtUtc));
            }
            else if (state == EntityState.Deleted)
            {
                events.Add(new LearnerWordRemoved(link.UserId, link.SenseId, nowUtc));
            }
        }

        return events;
    }
}
