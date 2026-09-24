using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;

public sealed class ModerateSharedVocabularyWordCommandValidator : AbstractValidator<ModerateSharedVocabularyWordCommand>
{
    public ModerateSharedVocabularyWordCommandValidator()
    {
        RuleFor(c => c.WordId).NotEmpty();
        RuleFor(c => c.ModeratorUserId).NotEmpty();
    }
}
