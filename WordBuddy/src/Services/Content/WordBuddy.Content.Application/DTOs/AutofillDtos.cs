using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>Result of an auto-fill lookup. <paramref name="AutofillUnavailable"/> = the UI shows the
/// manual form; then <paramref name="Senses"/> is empty.</summary>
public sealed record AutofillResultDto(string Word, bool AutofillUnavailable, IReadOnlyList<AutofillSenseDto> Senses)
{
    /// <summary>Unavailable result for <paramref name="word"/>.</summary>
    public static AutofillResultDto Unavailable(string word) => new(word, true, []);
}

/// <summary>One auto-filled sense card. <paramref name="AwaitingApproval"/> (Child only): no content is sent.</summary>
public sealed record AutofillSenseDto(
    Guid SenseId,
    string Word,
    string Definition,
    PartOfSpeech? PartOfSpeech,
    string? IpaUk,
    string? IpaUs,
    string? AudioUkUrl,
    string? AudioUsUrl,
    IReadOnlyList<string> Examples,
    IReadOnlyList<SenseTranslationDto> Translations,
    IReadOnlyList<string> Collocations,
    IReadOnlyList<string> Synonyms,
    IReadOnlyList<string> Antonyms,
    IReadOnlyList<string> TopicTags,
    string? RegisterNote,
    SenseOrigin Origin,
    bool AwaitingApproval)
{
    /// <summary>Full card from a sense with lexeme and translations loaded.</summary>
    public static AutofillSenseDto From(Sense sense)
    {
        SenseDetails d = SenseDetails.From(sense);
        return new AutofillSenseDto(
            sense.Id, sense.Word, sense.Definition, d.PartOfSpeech, d.IpaUk, d.IpaUs, d.AudioUkUrl, d.AudioUsUrl,
            d.Examples, d.Translations, d.Collocations, d.Synonyms, d.Antonyms, d.TopicTags, d.RegisterNote, d.Origin,
            AwaitingApproval: false);
    }

    /// <summary>The same card without content: word and part of speech only.</summary>
    public AutofillSenseDto Masked() => this with
    {
        Definition = string.Empty,
        IpaUk = null,
        IpaUs = null,
        AudioUkUrl = null,
        AudioUsUrl = null,
        Examples = [],
        Translations = [],
        Collocations = [],
        Synonyms = [],
        Antonyms = [],
        TopicTags = [],
        RegisterNote = null,
        AwaitingApproval = true,
    };
}

/// <summary>An auto-filled word waiting for child approval. <paramref name="LearnerId"/> is set in the
/// supporter queue, <see langword="null"/> in the admin queue. <paramref name="ChildSuitableHint"/> is
/// the generator's hint, for approvers only.</summary>
public sealed record ChildApprovalDto(
    Guid SenseId,
    Guid? LearnerId,
    string Word,
    string Definition,
    PartOfSpeech? PartOfSpeech,
    IReadOnlyList<string> Examples,
    IReadOnlyList<SenseTranslationDto> Translations,
    bool? ChildSuitableHint,
    DateTime CreatedAtUtc);
