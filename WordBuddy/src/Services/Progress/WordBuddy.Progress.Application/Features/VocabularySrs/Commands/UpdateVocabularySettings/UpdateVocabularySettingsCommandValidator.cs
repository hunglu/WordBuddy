using FluentValidation;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateVocabularySettings;

public sealed class UpdateVocabularySettingsCommandValidator : AbstractValidator<UpdateVocabularySettingsCommand>
{
    public UpdateVocabularySettingsCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.NewWordsPerDay)
            .InclusiveBetween(VocabularyLearnerSettings.MinNewWordsPerDay, VocabularyLearnerSettings.MaxNewWordsPerDay)
            .When(c => c.NewWordsPerDay.HasValue);
    }
}
