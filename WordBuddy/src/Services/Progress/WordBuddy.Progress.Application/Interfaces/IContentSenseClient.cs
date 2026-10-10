using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Reads senses from the Content service with the caller's identity, so Content applies the
/// child filter. Implemented in Infrastructure; Progress has no reference to Content.</summary>
public interface IContentSenseClient
{
    /// <summary>
    /// Visible senses for the session's words. Hidden and unknown ids are omitted. The reply is cached
    /// per session until <paramref name="sessionExpiresAtUtc"/>. Failure code <c>Content.Unavailable</c>
    /// when Content cannot be reached.
    /// </summary>
    Task<Result<IReadOnlyList<ContentSenseDto>>> GetSessionSensesAsync(
        Guid sessionId,
        IReadOnlyCollection<Guid> senseIds,
        DateTime sessionExpiresAtUtc,
        CancellationToken ct = default);
}
