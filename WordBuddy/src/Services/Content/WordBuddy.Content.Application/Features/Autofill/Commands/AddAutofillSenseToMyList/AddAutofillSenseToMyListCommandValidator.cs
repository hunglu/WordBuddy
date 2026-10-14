using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;

public sealed class AddAutofillSenseToMyListCommandValidator : AbstractValidator<AddAutofillSenseToMyListCommand>
{
    public AddAutofillSenseToMyListCommandValidator()
    {
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
        RuleFor(c => c.RequestingAgeGroup).IsInEnum();
        RuleFor(c => c.PersonalContext).Must(text => text is null || text.Trim().Length <= LearnerWord.PersonalContextMaxLength)
            .WithMessage($"The personal note must be at most {LearnerWord.PersonalContextMaxLength} characters.");
    }
}
