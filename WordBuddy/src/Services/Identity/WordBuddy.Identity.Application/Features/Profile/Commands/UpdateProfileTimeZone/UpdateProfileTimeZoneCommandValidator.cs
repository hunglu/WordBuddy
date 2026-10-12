using FluentValidation;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileTimeZone;

public sealed class UpdateProfileTimeZoneCommandValidator : AbstractValidator<UpdateProfileTimeZoneCommand>
{
    public UpdateProfileTimeZoneCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.TimeZoneId)
            .Must(z => User.IsKnownTimeZone(z!))
            .WithMessage("The time zone id is not known.")
            .When(c => !string.IsNullOrWhiteSpace(c.TimeZoneId));
    }
}
