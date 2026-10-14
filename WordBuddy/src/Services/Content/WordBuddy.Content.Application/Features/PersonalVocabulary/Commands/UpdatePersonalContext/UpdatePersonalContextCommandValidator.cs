using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalContext;

public sealed class UpdatePersonalContextCommandValidator : AbstractValidator<UpdatePersonalContextCommand>
{
    public UpdatePersonalContextCommandValidator()
    {
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.PersonalContext).MaximumLength(LearnerWord.PersonalContextMaxLength);
    }
}
