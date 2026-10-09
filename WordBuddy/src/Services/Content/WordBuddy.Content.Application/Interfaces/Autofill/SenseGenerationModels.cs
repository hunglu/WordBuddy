using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>Input of <see cref="ISenseGenerator"/>.</summary>
/// <param name="Word">The typed word.</param>
/// <param name="PartsOfSpeech">Parts of speech from the dictionary; empty lets the generator decide.</param>
/// <param name="TranslationLocales">BCP-47 locales to translate into (v1: <c>vi</c>).</param>
/// <param name="ExistingCatalogSenses">System or shared senses already stored for this lemma without a
/// part of speech. The generator may match them to a part of speech.</param>
public sealed record SenseGenerationRequest(
    string Word,
    IReadOnlyList<PartOfSpeech> PartsOfSpeech,
    IReadOnlyList<string> TranslationLocales,
    IReadOnlyList<ExistingCatalogSense> ExistingCatalogSenses);

/// <summary>A catalog sense the generator may match to a part of speech.</summary>
public sealed record ExistingCatalogSense(Guid SenseId, string Definition);

/// <summary>Output of <see cref="ISenseGenerator"/>: one entry per part of speech.</summary>
public sealed record GeneratedWord(IReadOnlyList<GeneratedLexeme> Lexemes);

/// <summary>Generated data for one part of speech.</summary>
/// <param name="MatchedExistingSenseIds">Ids from <see cref="SenseGenerationRequest.ExistingCatalogSenses"/>
/// that belong to this part of speech.</param>
public sealed record GeneratedLexeme(
    PartOfSpeech PartOfSpeech,
    string? Syllables,
    IReadOnlyList<string> WordForms,
    CefrLevel? CefrLevel,
    IReadOnlyList<GeneratedSense> Senses,
    IReadOnlyList<Guid> MatchedExistingSenseIds);

/// <summary>One generated meaning.</summary>
/// <param name="Translations">Locale → translated text.</param>
/// <param name="ChildSuitable">Generator's hint for approvers; never skips approval.</param>
public sealed record GeneratedSense(
    string Definition,
    IReadOnlyList<string> Examples,
    IReadOnlyDictionary<string, string> Translations,
    IReadOnlyList<string> Collocations,
    IReadOnlyList<string> Synonyms,
    IReadOnlyList<string> Antonyms,
    IReadOnlyList<string> TopicTags,
    string? RegisterNote,
    bool? ChildSuitable);
