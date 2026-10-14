using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalVocabularyWord;

/// <summary>Edits the caller's own private word. <paramref name="UserId"/> comes from the
/// authenticated caller's JWT — never trust a client-supplied owner.</summary>
public sealed record UpdatePersonalVocabularyWordCommand(
    Guid SenseId,
    Guid UserId,
    string Word,
    string Definition,
    string? Example) : ICommand;
