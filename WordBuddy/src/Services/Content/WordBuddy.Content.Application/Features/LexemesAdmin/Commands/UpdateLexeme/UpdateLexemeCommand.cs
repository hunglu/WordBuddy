using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UpdateLexeme;

/// <summary>Replaces the dictionary fields of a lexeme. The lemma does not change. The part of
/// speech is required.</summary>
public sealed record UpdateLexemeCommand(
    Guid LexemeId,
    PartOfSpeech PartOfSpeech,
    string? IpaUk,
    string? IpaUs,
    string? Syllables,
    IReadOnlyList<string>? WordForms,
    CefrLevel? CefrLevel,
    int? FrequencyRank) : ICommand;
