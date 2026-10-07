using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Stores <see cref="Domain.LearnerWordMembership"/> rows — one per (user, sense).</summary>
public interface ILearnerWordMembershipRepository
{
    /// <summary>Creates or re-activates the membership. Returns <see langword="true"/> when applied,
    /// <see langword="false"/> when the event was older than the stored state and ignored.
    /// <paramref name="beforeSave"/> runs only when the event is applied, right before the save, so
    /// changes it stages are saved in the same transaction.</summary>
    Task<Result<bool>> UpsertAddedAsync(
        Guid userId,
        Guid senseId,
        Guid addedBy,
        DateTime addedAtUtc,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct = default);

    /// <summary>Marks the membership inactive (keeps the row). An unknown membership gets an
    /// inactive row so an older add arriving later stays ignored. Returns <see langword="true"/>
    /// when applied, <see langword="false"/> when ignored. <paramref name="beforeSave"/> works as in
    /// <see cref="UpsertAddedAsync"/>.</summary>
    Task<Result<bool>> MarkRemovedAsync(
        Guid userId,
        Guid senseId,
        DateTime removedAtUtc,
        Func<LearnerWordMembership, CancellationToken, Task<Result>>? beforeSave,
        CancellationToken ct = default);
}
