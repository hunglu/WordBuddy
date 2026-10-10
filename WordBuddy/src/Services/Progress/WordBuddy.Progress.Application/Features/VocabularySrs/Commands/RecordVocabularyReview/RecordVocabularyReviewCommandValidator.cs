using FluentValidation;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;

public sealed class RecordVocabularyReviewCommandValidator : AbstractValidator<RecordVocabularyReviewCommand>
{
    /// <summary>Longest accepted response time (10 minutes).</summary>
    public const int MaxResponseMs = 600_000;

    /// <summary>Longest accepted typed answer.</summary>
    public const int MaxTextLength = 200;

    /// <summary>Longest accepted option key.</summary>
    public const int MaxOptionKeyLength = 64;

    public RecordVocabularyReviewCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ExerciseId).NotEmpty();
        RuleFor(c => c.AgeGroup).IsInEnum();
        RuleFor(c => c.ClientResponseMs).InclusiveBetween(0, MaxResponseMs);
        RuleFor(c => c.Answer).NotNull();
        When(c => c.Answer is not null, () =>
        {
            RuleFor(c => c.Answer.OptionKey).MaximumLength(MaxOptionKeyLength);
            RuleFor(c => c.Answer.Text).MaximumLength(MaxTextLength);
            RuleFor(c => c.Answer)
                .Must(a => !string.IsNullOrWhiteSpace(a.OptionKey) || !string.IsNullOrWhiteSpace(a.Text))
                .WithMessage("An answer needs an option key or text.");
        });
    }
}
