using FluentValidation;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RenameGroup;

public sealed class RenameGroupCommandValidator : AbstractValidator<RenameGroupCommand>
{
    public RenameGroupCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.CallerId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().Must(n => n.Trim().Length is >= LearnerGroup.NameMinLength and <= LearnerGroup.NameMaxLength)
            .WithMessage("The group name must be 3 to 60 characters.");
    }
}
