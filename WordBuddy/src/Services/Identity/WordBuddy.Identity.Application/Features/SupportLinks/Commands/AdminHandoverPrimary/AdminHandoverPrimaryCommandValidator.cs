using FluentValidation;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;

public sealed class AdminHandoverPrimaryCommandValidator : AbstractValidator<AdminHandoverPrimaryCommand>
{
    public AdminHandoverPrimaryCommandValidator()
    {
        RuleFor(c => c.AdminId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEmpty();
        RuleFor(c => c.NewPrimaryLinkId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(SupportLinkAuditEntry.ReasonMaxLength);
    }
}
