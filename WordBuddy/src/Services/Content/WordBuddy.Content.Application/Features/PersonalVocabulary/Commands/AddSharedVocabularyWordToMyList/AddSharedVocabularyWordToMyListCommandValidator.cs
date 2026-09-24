using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;

public sealed class AddSharedVocabularyWordToMyListCommandValidator : AbstractValidator<AddSharedVocabularyWordToMyListCommand>
{
    public AddSharedVocabularyWordToMyListCommandValidator()
    {
        RuleFor(c => c.SharedWordId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
        RuleFor(c => c.RequestingAgeGroup).IsInEnum();
    }
}
