using FluentValidation;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.CreateGroup;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(c => c.OwnerId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().Must(n => n.Trim().Length is >= LearnerGroup.NameMinLength and <= LearnerGroup.NameMaxLength)
            .WithMessage("The group name must be 3 to 60 characters.");
    }
}
