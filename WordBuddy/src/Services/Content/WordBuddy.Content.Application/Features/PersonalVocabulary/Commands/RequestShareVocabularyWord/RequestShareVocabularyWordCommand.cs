using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;

/// <summary><paramref name="RequestingUserId"/> comes from the authenticated caller's JWT.</summary>
public sealed record RequestShareVocabularyWordCommand(Guid WordId, Guid RequestingUserId) : ICommand;
