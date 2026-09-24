using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;

/// <summary><paramref name="RequestingUserId"/>/<paramref name="RequestingAgeGroup"/> come from the
/// authenticated caller's JWT.</summary>
public sealed record AddSharedVocabularyWordToMyListCommand(
    Guid SharedWordId,
    Guid RequestingUserId,
    AgeGroup RequestingAgeGroup) : ICommand<Guid>;
