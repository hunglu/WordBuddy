using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Pulls word-id remaps from Content's internal endpoint and acknowledges applied ones.
/// Expected failures come back as <see cref="ContentApiErrors"/>, never as exceptions.</summary>
public interface IContentVocabularyRemapClient
{
    /// <summary>Returns at most <paramref name="limit"/> pending remaps, ordered by old id.</summary>
    Task<Result<IReadOnlyList<VocabularyWordIdRemapPair>>> GetPendingAsync(int limit, CancellationToken ct = default);

    /// <summary>Tells Content the remaps for <paramref name="oldIds"/> have been applied.</summary>
    Task<Result> AcknowledgeAsync(IReadOnlyList<Guid> oldIds, CancellationToken ct = default);
}

/// <summary>Typed errors for calls to the Content API.</summary>
public static class ContentApiErrors
{
    /// <summary>Content couldn't be reached, timed out, returned a server error, or doesn't have the
    /// endpoint yet (404 — not migrated/deployed).</summary>
    public static Error Unavailable(string description) => Error.Failure("ContentApi.Unavailable", description);

    /// <summary>Content rejected the service token (401/403).</summary>
    public static Error Unauthorized(string description) => Error.Failure("ContentApi.Unauthorized", description);
}
