using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>Projects <see cref="Sense"/>s and <see cref="LearnerWord"/> links onto
/// <see cref="PersonalVocabularyWordDto"/>, keeping the values callers saw before words were unified.</summary>
public static class PersonalVocabularyWordMapper
{
    /// <summary>Pool / moderation view: the word as it is.</summary>
    public static PersonalVocabularyWordDto ToDto(Sense word) => WithDetails(
        new PersonalVocabularyWordDto(
            word.Id,
            word.OwnerUserId,
            word.Word,
            word.Definition,
            word.Example,
            word.ShareStatus,
            word.VisibleToChildren,
            word.CreatedAtUtc,
            IsAuthor: false),
        SenseDetails.From(word));

    /// <summary>Caller-scoped view through a link (adult rules).</summary>
    public static PersonalVocabularyWordDto ToDto(LearnerWord link) => ToDto(link, AgeGroup.Adult);

    /// <summary>Caller-scoped view through a link. For words the caller didn't write, the values
    /// match the old private copy: owner = caller, <see cref="VocabularyShareStatus.Private"/>, not
    /// child-visible, created when the link was added. A Child caller waiting for approval of an
    /// auto-filled word gets <c>AwaitingApproval = true</c> and no content.</summary>
    public static PersonalVocabularyWordDto ToDto(LearnerWord link, AgeGroup callerAgeGroup)
    {
        Sense word = link.Sense
            ?? throw new InvalidOperationException($"Link {link.Id} was loaded without its word.");

        PersonalVocabularyWordDto dto = link.IsAuthor
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

        if (word.IsAwaitingChildApproval(callerAgeGroup, link))
        {
            return WithDetails(dto with { Definition = string.Empty, Example = null, AwaitingApproval = true }, SenseDetails.Empty(word.Origin));
        }

        return WithDetails(dto, SenseDetails.From(word));
    }

    private static PersonalVocabularyWordDto WithDetails(PersonalVocabularyWordDto dto, SenseDetails details) => dto with
    {
        PartOfSpeech = details.PartOfSpeech,
        IpaUk = details.IpaUk,
        IpaUs = details.IpaUs,
        AudioUkUrl = details.AudioUkUrl,
        AudioUsUrl = details.AudioUsUrl,
        Examples = details.Examples,
        Translations = details.Translations,
        Collocations = details.Collocations,
        Synonyms = details.Synonyms,
        Antonyms = details.Antonyms,
        TopicTags = details.TopicTags,
        RegisterNote = details.RegisterNote,
        Origin = details.Origin,
    };
}
