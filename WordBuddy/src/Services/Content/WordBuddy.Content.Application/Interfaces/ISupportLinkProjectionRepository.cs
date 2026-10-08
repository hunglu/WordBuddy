using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Stores <see cref="SupportLinkProjection"/> rows and answers the support policies.</summary>
public interface ISupportLinkProjectionRepository
{
    /// <summary>Returns the tracked projection, or not found.</summary>
    Task<Result<SupportLinkProjection>> GetTrackedAsync(Guid linkId, CancellationToken ct = default);

    /// <summary>Stages a new projection.</summary>
    Task AddAsync(SupportLinkProjection projection, CancellationToken ct = default);

    /// <summary>Saves staged changes.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Whether <paramref name="supporterId"/> has an active link to <paramref name="learnerId"/>.</summary>
    Task<bool> HasActiveLinkAsync(Guid supporterId, Guid learnerId, CancellationToken ct = default);

    /// <summary>Whether the learner has at least one active supporter.</summary>
    Task<bool> HasActiveSupporterAsync(Guid learnerId, CancellationToken ct = default);
}
