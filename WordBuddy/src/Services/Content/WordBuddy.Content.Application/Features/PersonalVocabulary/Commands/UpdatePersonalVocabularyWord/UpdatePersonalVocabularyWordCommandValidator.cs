using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalVocabularyWord;

public sealed class UpdatePersonalVocabularyWordCommandValidator : AbstractValidator<UpdatePersonalVocabularyWordCommand>
{
    public UpdatePersonalVocabularyWordCommandValidator()
    {
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Word).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Definition).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Example).MaximumLength(500);
    }
}
