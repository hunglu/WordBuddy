using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.DeleteGroup;

public sealed class DeleteGroupCommandValidator : AbstractValidator<DeleteGroupCommand>
{
    public DeleteGroupCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.CallerId).NotEmpty();
    }
}
