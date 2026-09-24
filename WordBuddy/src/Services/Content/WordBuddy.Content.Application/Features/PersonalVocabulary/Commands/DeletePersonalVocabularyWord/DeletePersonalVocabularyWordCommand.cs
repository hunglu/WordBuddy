using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;

/// <summary><paramref name="RequestingUserId"/> comes from the authenticated caller's JWT.</summary>
public sealed record DeletePersonalVocabularyWordCommand(Guid WordId, Guid RequestingUserId) : ICommand;
