using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;

public sealed class AddPersonalVocabularyWordCommandValidator : AbstractValidator<AddPersonalVocabularyWordCommand>
{
    public AddPersonalVocabularyWordCommandValidator()
    {
        RuleFor(c => c.OwnerUserId).NotEmpty();
        RuleFor(c => c.OwnerAgeGroup).IsInEnum();
        RuleFor(c => c.Word).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Definition).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Example).MaximumLength(500);
    }
}
