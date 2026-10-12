using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges;

/// <summary>
/// Removes Guardian consents and, for a child left without any consent, the child participations in
/// challenges that are not finished. Saves its changes and deletes the affected board caches.
/// </summary>
public interface IChallengeConsentCleaner
{
    /// <summary>The support link between <paramref name="learnerId"/> and <paramref name="supporterId"/> ended.</summary>
    Task<Result> RemoveForLinkAsync(Guid learnerId, Guid supporterId, CancellationToken ct = default);

    /// <summary>The Guardian role of <paramref name="guardianId"/> ended: drop every consent that guardian gave.</summary>
    Task<Result> RemoveByGuardianAsync(Guid guardianId, CancellationToken ct = default);

    /// <summary>A guardian switched challenges off for the child: drop every consent of the child.</summary>
    Task<Result> RemoveAllForChildAsync(Guid childId, CancellationToken ct = default);
}
