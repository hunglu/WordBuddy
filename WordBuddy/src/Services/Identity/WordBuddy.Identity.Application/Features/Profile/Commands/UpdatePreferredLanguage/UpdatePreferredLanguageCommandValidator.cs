using FluentValidation;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdatePreferredLanguage;

public sealed class UpdatePreferredLanguageCommandValidator : AbstractValidator<UpdatePreferredLanguageCommand>
{
    public UpdatePreferredLanguageCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Language)
            .Must(l => User.IsValidLanguageCode(l!))
            .WithMessage("The language code is not valid.")
            .When(c => !string.IsNullOrWhiteSpace(c.Language));
    }
}
