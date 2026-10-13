using FluentValidation;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandValidator : AbstractValidator<UpdateDisplayNameCommand>
{
    public UpdateDisplayNameCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.DisplayName)
            .NotEmpty()
            .Must(n => n.Trim().Length is >= User.DisplayNameMinLength and <= User.DisplayNameMaxLength)
            .WithMessage("The display name must be 2 to 50 characters.");
    }
}
