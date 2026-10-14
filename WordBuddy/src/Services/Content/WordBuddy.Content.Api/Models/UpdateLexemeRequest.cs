using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Api.Models;

/// <summary>Request body to correct the dictionary fields of a lexeme. The part of speech is required;
/// the other fields replace the stored values, and an empty value clears the field.</summary>
public sealed record UpdateLexemeRequest(
    PartOfSpeech? PartOfSpeech,
    string? IpaUk,
    string? IpaUs,
    string? Syllables,
    IReadOnlyList<string>? WordForms,
    CefrLevel? CefrLevel,
    int? FrequencyRank);
