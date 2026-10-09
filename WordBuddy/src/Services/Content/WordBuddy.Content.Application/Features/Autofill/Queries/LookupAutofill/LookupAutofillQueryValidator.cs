using FluentValidation;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.LookupAutofill;

/// <summary>One English word or short phrase: letters, spaces, hyphens and apostrophes, at most 60 characters.</summary>
public sealed class LookupAutofillQueryValidator : AbstractValidator<LookupAutofillQuery>
{
    /// <summary>Maximum word length.</summary>
    public const int MaxWordLength = 60;

    public LookupAutofillQueryValidator()
    {
        RuleFor(q => q.Word)
            .NotEmpty()
            .MaximumLength(MaxWordLength)
            .Matches(@"^\s*[A-Za-z][A-Za-z' \-]*\s*$")
            .WithMessage("Enter one English word or short phrase (letters, spaces, hyphens, apostrophes).");
        RuleFor(q => q.RequestingUserId).NotEmpty();
        RuleFor(q => q.RequestingAgeGroup).IsInEnum();
    }
}
