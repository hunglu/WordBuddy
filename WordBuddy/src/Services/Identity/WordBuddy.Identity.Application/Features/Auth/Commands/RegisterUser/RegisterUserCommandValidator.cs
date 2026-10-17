using FluentValidation;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.AgeGroup).IsInEnum();
        RuleFor(c => c.TimeZoneId)
            .Must(z => User.IsKnownTimeZone(z!))
            .WithMessage("The time zone id is not known.")
            .When(c => !string.IsNullOrWhiteSpace(c.TimeZoneId));
        RuleFor(c => c.PreferredLanguage)
            .Must(l => User.IsValidLanguageCode(l!))
            .WithMessage("The language code is not valid.")
            .When(c => !string.IsNullOrWhiteSpace(c.PreferredLanguage));
    }
}
