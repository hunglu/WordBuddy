using FluentValidation;

namespace WordBuddy.Content.Application.Features.Groups.Commands.AssignWordsToGroup;

public sealed class AssignWordsToGroupCommandValidator : AbstractValidator<AssignWordsToGroupCommand>
{
    /// <summary>Most senses in one call.</summary>
    public const int MaxSensesPerCall = 50;

    public AssignWordsToGroupCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.CallerId).NotEmpty();
        RuleFor(c => c.SenseIds).NotNull().NotEmpty().Must(ids => ids.Count <= MaxSensesPerCall)
            .WithMessage("Assign at most 50 words per call.");
        RuleForEach(c => c.SenseIds).NotEmpty();
    }
}
