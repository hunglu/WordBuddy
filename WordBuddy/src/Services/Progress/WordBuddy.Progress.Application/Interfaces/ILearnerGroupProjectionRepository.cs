using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Stores <see cref="LearnerGroupMemberProjection"/> rows.</summary>
public interface ILearnerGroupProjectionRepository
{
    /// <summary>Returns the tracked row of a member, or not found.</summary>
    Task<Result<LearnerGroupMemberProjection>> GetTrackedAsync(Guid groupId, Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns every tracked row of a group (active or not).</summary>
    Task<IReadOnlyList<LearnerGroupMemberProjection>> GetGroupTrackedAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Returns every row of a group (read-only).</summary>
    Task<IReadOnlyList<LearnerGroupMemberProjection>> GetGroupAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Whether <paramref name="ownerId"/> owns the group (any row of the group names them as owner).</summary>
    Task<bool> IsOwnerAsync(Guid groupId, Guid ownerId, CancellationToken ct = default);

    /// <summary>Stages a new row.</summary>
    Task AddAsync(LearnerGroupMemberProjection projection, CancellationToken ct = default);

    /// <summary>Saves staged changes.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
