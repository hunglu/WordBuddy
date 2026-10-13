using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDateOfBirth;

public sealed class UpdateDateOfBirthCommandValidator : AbstractValidator<UpdateDateOfBirthCommand>
{
    public UpdateDateOfBirthCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
