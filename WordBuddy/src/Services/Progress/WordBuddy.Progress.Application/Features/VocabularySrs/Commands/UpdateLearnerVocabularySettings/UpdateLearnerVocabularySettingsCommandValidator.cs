using FluentValidation;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateLearnerVocabularySettings;

public sealed class UpdateLearnerVocabularySettingsCommandValidator : AbstractValidator<UpdateLearnerVocabularySettingsCommand>
{
    public UpdateLearnerVocabularySettingsCommandValidator()
    {
        RuleFor(c => c.SupporterId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEmpty();
        RuleFor(c => c.SupporterNewWordCap)
            .InclusiveBetween(VocabularyLearnerSettings.MinNewWordsPerDay, VocabularyLearnerSettings.MaxNewWordsPerDay)
            .When(c => c.SupporterNewWordCap.HasValue);
    }
}
