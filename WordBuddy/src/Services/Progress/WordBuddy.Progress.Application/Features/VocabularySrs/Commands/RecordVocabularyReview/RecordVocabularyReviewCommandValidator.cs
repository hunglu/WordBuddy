using FluentValidation;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;

public sealed class RecordVocabularyReviewCommandValidator : AbstractValidator<RecordVocabularyReviewCommand>
{
    /// <summary>Longest accepted response time (10 minutes).</summary>
    public const int MaxResponseMs = 600_000;

    public RecordVocabularyReviewCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.SessionId).NotEmpty();
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.AgeGroup).IsInEnum();
        RuleFor(c => c.ExerciseType).IsInEnum();
        RuleFor(c => c.Skill).IsInEnum();
        RuleFor(c => c.ResponseMs).InclusiveBetween(0, MaxResponseMs);
    }
}
