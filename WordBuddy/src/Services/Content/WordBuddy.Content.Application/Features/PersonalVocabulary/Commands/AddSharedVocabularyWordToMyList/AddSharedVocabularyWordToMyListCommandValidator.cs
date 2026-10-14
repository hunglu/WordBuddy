using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;

public sealed class AddSharedVocabularyWordToMyListCommandValidator : AbstractValidator<AddSharedVocabularyWordToMyListCommand>
{
    public AddSharedVocabularyWordToMyListCommandValidator()
    {
        RuleFor(c => c.SharedWordId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
        RuleFor(c => c.RequestingAgeGroup).IsInEnum();
        RuleFor(c => c.PersonalContext).Must(text => text is null || text.Trim().Length <= LearnerWord.PersonalContextMaxLength)
            .WithMessage($"The personal note must be at most {LearnerWord.PersonalContextMaxLength} characters.");
    }
}
