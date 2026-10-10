using FluentValidation;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.CreateVocabularyExercise;

public sealed class CreateVocabularyExerciseCommandValidator : AbstractValidator<CreateVocabularyExerciseCommand>
{
    public CreateVocabularyExerciseCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.SessionId).NotEmpty();
        RuleFor(c => c.SenseId).NotEmpty();
    }
}
