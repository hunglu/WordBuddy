using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;

/// <summary><paramref name="RequestingUserId"/> comes from the authenticated caller's JWT.
/// <paramref name="Confirm"/> must be <see langword="true"/> when the author deletes a
/// <c>Shared</c> or <c>PendingReview</c> word; otherwise the handler returns a conflict.</summary>
public sealed record DeletePersonalVocabularyWordCommand(Guid WordId, Guid RequestingUserId, bool Confirm = false) : ICommand;
