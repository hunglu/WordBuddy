using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>Projects <see cref="VocabularyWord"/>s and <see cref="UserVocabularyWord"/> links onto
/// <see cref="PersonalVocabularyWordDto"/>, keeping the values callers saw before words were unified.</summary>
public static class PersonalVocabularyWordMapper
{
    /// <summary>Pool / moderation view: the word as it is.</summary>
    public static PersonalVocabularyWordDto ToDto(VocabularyWord word) => new(
        word.Id,
        word.OwnerUserId,
        word.Word,
        word.Definition,
        word.Example,
        word.ShareStatus,
        word.VisibleToChildren,
        word.CreatedAtUtc,
        IsAuthor: false);

    /// <summary>Caller-scoped view through a link. For words the caller didn't write, the values
    /// match the old private copy: owner = caller, <see cref="VocabularyShareStatus.Private"/>, not
    /// child-visible, created when the link was added.</summary>
    public static PersonalVocabularyWordDto ToDto(UserVocabularyWord link)
    {
        VocabularyWord word = link.VocabularyWord
            ?? throw new InvalidOperationException($"Link {link.Id} was loaded without its word.");

        return link.IsAuthor
            ? new PersonalVocabularyWordDto(
                word.Id,
                word.OwnerUserId,
                word.Word,
                word.Definition,
                word.Example,
                word.ShareStatus,
                word.VisibleToChildren,
                word.CreatedAtUtc,
                IsAuthor: true)
            : new PersonalVocabularyWordDto(
                word.Id,
                link.UserId,
                word.Word,
                word.Definition,
                word.Example,
                VocabularyShareStatus.Private,
                VisibleToChildren: false,
                link.AddedAtUtc,
                IsAuthor: false);
    }
}
