using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>Persistence for <see cref="Sense"/>s, their <see cref="Lexeme"/>s, and the per-learner
/// <see cref="LearnerWord"/> links.</summary>
public interface IVocabularyWordRepository
{
    /// <summary>Returns every sense with the given <see cref="Sense.ContentHash"/>, ordered by
    /// id (SQL Server order) — may hold system senses, shared senses and other learners' private senses;
    /// the caller decides which, if any, it may link to.</summary>
    Task<Result<IReadOnlyList<Sense>>> FindByContentHashAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Returns a change-tracked sense by id, or <see cref="Error.NotFound"/>.</summary>
    Task<Result<Sense>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the caller's link to a sense (sense loaded), or <see cref="Error.NotFound"/> —
    /// the same error whether the sense doesn't exist or just isn't in the caller's list, so a caller
    /// can never probe for other users' word ids.</summary>
    Task<Result<LearnerWord>> GetLinkAsync(Guid userId, Guid senseId, CancellationToken ct = default);

    /// <summary>Returns all of a learner's links with their senses loaded, newest first.</summary>
    Task<Result<IReadOnlyList<LearnerWord>>> GetLinkedToUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns one page of learner links (ids and add time only), ordered by link id, skipping
    /// <see cref="SystemOwner"/> links. Keyset paging: only links with an id after
    /// <paramref name="afterLinkId"/> (<c>null</c> = from the start). Used by the backfill.</summary>
    Task<Result<IReadOnlyList<LearnerWordLink>>> GetLearnerLinksPageAsync(Guid? afterLinkId, int take, CancellationToken ct = default);

    /// <summary>Returns a random subset (at most <paramref name="count"/>) of a learner's links, senses loaded.</summary>
    Task<Result<IReadOnlyList<LearnerWord>>> GetRandomLinkedToUserAsync(Guid userId, int count, CancellationToken ct = default);

    /// <summary>Returns <see cref="VocabularyShareStatus.Shared"/> senses, additionally filtered to
    /// <see cref="Sense.VisibleToChildren"/> when <paramref name="childSafeOnly"/> is <see langword="true"/>.</summary>
    Task<Result<IReadOnlyList<Sense>>> GetSharedAsync(bool childSafeOnly, CancellationToken ct = default);

    /// <summary>Returns the senses with the given ids (audio and image loaded), each with
    /// <paramref name="userId"/>'s link to it or <see langword="null"/>, in one round trip. Unknown ids
    /// are omitted. No visibility filter: the caller applies <see cref="Sense.IsVisibleTo"/>.</summary>
    Task<Result<IReadOnlyList<SenseReviewCandidate>>> GetForReviewAsync(Guid userId, IReadOnlyCollection<Guid> senseIds, CancellationToken ct = default);

    /// <summary>Returns senses awaiting moderation, oldest first.</summary>
    Task<Result<IReadOnlyList<Sense>>> GetPendingModerationAsync(CancellationToken ct = default);

    /// <summary>Returns the <see cref="Lexeme"/> for <paramref name="word"/> with an unknown
    /// (<see langword="null"/>) part of speech, creating it when missing. Safe under concurrency:
    /// parallel calls for the same normalized word return the same lexeme id.</summary>
    Task<Result<Lexeme>> GetOrCreateLexemeAsync(string word, CancellationToken ct = default);

    /// <summary>Adds a new sense together with its author's link, in one save, and returns its id. If a
    /// concurrent request by the same owner already stored a sense with the same content hash, nothing
    /// new is stored and the existing sense's id is returned (the owner linked to it) — the same outcome
    /// as the non-racing dedupe path. If the sense's lexeme was deleted in between, the lexeme is
    /// re-created once and the add retried.</summary>
    Task<Result<Guid>> AddAsync(Sense word, LearnerWord authorLink, CancellationToken ct = default);

    /// <summary>Persists changes made to a sense previously returned by <see cref="GetByIdAsync"/>.</summary>
    Task<Result> UpdateAsync(Sense word, CancellationToken ct = default);

    /// <summary>Adds a link from a learner to an existing sense. Idempotent under concurrency: if the
    /// same link was created concurrently, succeeds without adding a second one.</summary>
    Task<Result> LinkAsync(LearnerWord link, CancellationToken ct = default);

    /// <summary>Removes a link previously returned by <see cref="GetLinkAsync"/>.</summary>
    Task<Result> UnlinkAsync(LearnerWord link, CancellationToken ct = default);

    /// <summary>Deletes the sense when it is <see cref="VocabularySource.Learner"/>-sourced, not
    /// <see cref="VocabularyShareStatus.Shared"/>, and no learner or lesson links to it any more. Its
    /// <see cref="Lexeme"/> is deleted too when no other sense references it. Returns whether the
    /// sense was deleted.</summary>
    Task<Result<bool>> DeleteIfOrphanedAsync(Guid senseId, CancellationToken ct = default);
}
