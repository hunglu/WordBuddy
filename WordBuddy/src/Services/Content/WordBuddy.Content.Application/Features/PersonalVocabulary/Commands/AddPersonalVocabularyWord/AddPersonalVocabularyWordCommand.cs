using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;

/// <summary><paramref name="OwnerUserId"/> and <paramref name="OwnerAgeGroup"/> come from the
/// authenticated caller's JWT — never trust a client-supplied owner.</summary>
public sealed record AddPersonalVocabularyWordCommand(
    Guid OwnerUserId,
    AgeGroup OwnerAgeGroup,
    string Word,
    string Definition,
    string? Example) : ICommand<Guid>;
