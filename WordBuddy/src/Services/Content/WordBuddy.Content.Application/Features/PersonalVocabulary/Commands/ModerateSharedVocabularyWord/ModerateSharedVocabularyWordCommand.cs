using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;

/// <summary><paramref name="ModeratorUserId"/> comes from the authenticated (admin-only) caller's
/// JWT. <paramref name="VisibleToChildren"/> is only meaningful when <paramref name="Approve"/> is
/// <see langword="true"/>.</summary>
public sealed record ModerateSharedVocabularyWordCommand(
    Guid WordId,
    bool Approve,
    bool VisibleToChildren,
    Guid ModeratorUserId) : ICommand;
