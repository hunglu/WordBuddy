using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalContext;

public sealed class UpdatePersonalContextCommandValidator : AbstractValidator<UpdatePersonalContextCommand>
{
    public UpdatePersonalContextCommandValidator()
    {
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.PersonalContext).Must(text => text is null || text.Trim().Length <= LearnerWord.PersonalContextMaxLength)
            .WithMessage($"The personal note must be at most {LearnerWord.PersonalContextMaxLength} characters.");
    }
}
