using FluentValidation;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;

public sealed class AddAutofillSenseToMyListCommandValidator : AbstractValidator<AddAutofillSenseToMyListCommand>
{
    public AddAutofillSenseToMyListCommandValidator()
    {
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
        RuleFor(c => c.RequestingAgeGroup).IsInEnum();
    }
}
