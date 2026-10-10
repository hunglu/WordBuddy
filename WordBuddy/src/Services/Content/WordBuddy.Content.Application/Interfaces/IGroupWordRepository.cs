using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Reads and writes for a group word assignment. One <see cref="SaveChangesAsync"/> stores the
/// links and the history rows; the context publishes <c>LearnerWordAdded</c> for each new link in the same transaction.</summary>
public interface IGroupWordRepository
{
    /// <summary>Returns the senses with the given ids (read-only). Unknown ids are omitted.</summary>
    Task<IReadOnlyList<Sense>> GetSensesAsync(IReadOnlyCollection<Guid> senseIds, CancellationToken ct = default);

    /// <summary>Returns the (learner, sense) pairs that already have a link.</summary>
    Task<IReadOnlySet<(Guid UserId, Guid SenseId)>> GetExistingLinksAsync(
        IReadOnlyCollection<Guid> userIds, IReadOnlyCollection<Guid> senseIds, CancellationToken ct = default);

    /// <summary>Returns the assignments of a group, newest first (read-only).</summary>
    Task<IReadOnlyList<GroupWordAssignment>> GetAssignmentsAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Stages new learner links.</summary>
    Task AddLinksAsync(IReadOnlyCollection<LearnerWord> links, CancellationToken ct = default);

    /// <summary>Stages new assignment rows.</summary>
    Task AddAssignmentsAsync(IReadOnlyCollection<GroupWordAssignment> assignments, CancellationToken ct = default);

    /// <summary>Commits all staged changes in one transaction.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
