using FluentValidation;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.ApproveChildWord;

public sealed class ApproveChildWordCommandValidator : AbstractValidator<ApproveChildWordCommand>
{
    public ApproveChildWordCommandValidator()
    {
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.ApproverId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEqual(Guid.Empty).When(c => c.LearnerId is not null);
    }
}
