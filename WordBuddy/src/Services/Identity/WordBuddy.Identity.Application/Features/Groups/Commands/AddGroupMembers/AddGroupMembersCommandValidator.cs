using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.AddGroupMembers;

public sealed class AddGroupMembersCommandValidator : AbstractValidator<AddGroupMembersCommand>
{
    /// <summary>Most learners in one call.</summary>
    public const int MaxLearnersPerCall = 100;

    public AddGroupMembersCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.CallerId).NotEmpty();
        RuleFor(c => c.LearnerIds).NotNull().NotEmpty().Must(ids => ids.Count <= MaxLearnersPerCall)
            .WithMessage("Add at most 100 learners per call.");
        RuleForEach(c => c.LearnerIds).NotEmpty();
    }
}
