using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Persistence for <see cref="VocabularyWord"/> and its per-learner
/// <see cref="UserVocabularyWord"/> links.</summary>
public interface IVocabularyWordRepository
{
    /// <summary>Returns every word with the given <see cref="VocabularyWord.ContentHash"/>, ordered by
    /// id (SQL Server order) — may hold system words, shared words and other learners' private words;
    /// the caller decides which, if any, it may link to.</summary>
    Task<Result<IReadOnlyList<VocabularyWord>>> FindByContentHashAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Returns a change-tracked word by id, or <see cref="Error.NotFound"/>.</summary>
    Task<Result<VocabularyWord>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the caller's link to a word (word loaded), or <see cref="Error.NotFound"/> —
    /// the same error whether the word doesn't exist or just isn't in the caller's list, so a caller
    /// can never probe for other users' word ids.</summary>
    Task<Result<UserVocabularyWord>> GetLinkAsync(Guid userId, Guid vocabularyWordId, CancellationToken ct = default);

    /// <summary>Returns all of a learner's links with their words loaded, newest first.</summary>
    Task<Result<IReadOnlyList<UserVocabularyWord>>> GetLinkedToUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns a random subset (at most <paramref name="count"/>) of a learner's links, words loaded.</summary>
    Task<Result<IReadOnlyList<UserVocabularyWord>>> GetRandomLinkedToUserAsync(Guid userId, int count, CancellationToken ct = default);

    /// <summary>Returns <see cref="VocabularyShareStatus.Shared"/> words, additionally filtered to
    /// <see cref="VocabularyWord.VisibleToChildren"/> when <paramref name="childSafeOnly"/> is <see langword="true"/>.</summary>
    Task<Result<IReadOnlyList<VocabularyWord>>> GetSharedAsync(bool childSafeOnly, CancellationToken ct = default);

    /// <summary>Returns words awaiting moderation, oldest first.</summary>
    Task<Result<IReadOnlyList<VocabularyWord>>> GetPendingModerationAsync(CancellationToken ct = default);

    /// <summary>Adds a new word together with its author's link, in one save, and returns its id. If a
    /// concurrent request by the same owner already stored a word with the same content hash, nothing
    /// new is stored and the existing word's id is returned (the owner linked to it) — the same outcome
    /// as the non-racing dedupe path.</summary>
    Task<Result<Guid>> AddAsync(VocabularyWord word, UserVocabularyWord authorLink, CancellationToken ct = default);

    /// <summary>Persists changes made to a word previously returned by <see cref="GetByIdAsync"/>.</summary>
    Task<Result> UpdateAsync(VocabularyWord word, CancellationToken ct = default);

    /// <summary>Adds a link from a learner to an existing word. Idempotent under concurrency: if the
    /// same link was created concurrently, succeeds without adding a second one.</summary>
    Task<Result> LinkAsync(UserVocabularyWord link, CancellationToken ct = default);

    /// <summary>Removes a link previously returned by <see cref="GetLinkAsync"/>.</summary>
    Task<Result> UnlinkAsync(UserVocabularyWord link, CancellationToken ct = default);

    /// <summary>Deletes the word when it is <see cref="VocabularySource.Learner"/>-sourced, not
    /// <see cref="VocabularyShareStatus.Shared"/>, and no learner or lesson links to it any more.
    /// Returns whether it was deleted.</summary>
    Task<Result<bool>> DeleteIfOrphanedAsync(Guid vocabularyWordId, CancellationToken ct = default);
}
