using System.Text.RegularExpressions;
using FluentValidation;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileAliasAvatar;

public sealed partial class UpdateProfileAliasAvatarCommandValidator : AbstractValidator<UpdateProfileAliasAvatarCommand>
{
    public UpdateProfileAliasAvatarCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Alias)
            .Must(a => a!.Trim().Length is >= User.AliasMinLength and <= User.AliasMaxLength)
            .WithMessage("The alias must be 3 to 20 characters.")
            .Must(a => AliasPattern().IsMatch(a!.Trim()))
            .WithMessage("The alias may use letters, digits, space, dot, dash and underscore.")
            .When(c => !string.IsNullOrWhiteSpace(c.Alias));
        RuleFor(c => c.AvatarId)
            .Must(a => AvatarCatalog.Contains(a!))
            .WithMessage("The avatar is not in the catalog.")
            .When(c => c.AvatarId is not null);
    }

    [GeneratedRegex("^[A-Za-z0-9_ .-]+$")]
    private static partial Regex AliasPattern();
}
