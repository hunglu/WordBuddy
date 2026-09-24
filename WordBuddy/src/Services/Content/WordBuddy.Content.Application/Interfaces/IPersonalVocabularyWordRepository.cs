using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

public interface IPersonalVocabularyWordRepository
{
    Task<Result> AddAsync(PersonalVocabularyWord word, CancellationToken ct = default);

    /// <summary>Returns a change-tracked word if it exists and is owned by <paramref name="ownerUserId"/>,
    /// or <see cref="Error.NotFound"/> — never distinguishes "doesn't exist" from "not owned by you",
    /// so a caller can never probe for another user's word ids.</summary>
    Task<Result<PersonalVocabularyWord>> GetOwnedByIdAsync(Guid id, Guid ownerUserId, CancellationToken ct = default);

    /// <summary>Returns a word by id regardless of owner, or <see cref="Error.NotFound"/> — used when
    /// copying a <see cref="VocabularyShareStatus.Shared"/> pool word into another user's list.</summary>
    Task<Result<PersonalVocabularyWord>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>Returns a random subset (at most <paramref name="count"/>) of the caller's own words.</summary>
    Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetRandomByOwnerAsync(Guid ownerUserId, int count, CancellationToken ct = default);

    /// <summary>Returns <see cref="VocabularyShareStatus.Shared"/> words, additionally filtered to
    /// <see cref="PersonalVocabularyWord.VisibleToChildren"/> when <paramref name="childSafeOnly"/> is <see langword="true"/>.</summary>
    Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetSharedAsync(bool childSafeOnly, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetPendingModerationAsync(CancellationToken ct = default);

    /// <summary>Persists changes made to an entity previously returned by a tracked query
    /// (<see cref="GetOwnedByIdAsync"/>/<see cref="GetByIdAsync"/>).</summary>
    Task<Result> UpdateAsync(PersonalVocabularyWord word, CancellationToken ct = default);

    Task<Result> DeleteAsync(PersonalVocabularyWord word, CancellationToken ct = default);
}
