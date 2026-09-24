using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;

public sealed class DeletePersonalVocabularyWordCommandValidator : AbstractValidator<DeletePersonalVocabularyWordCommand>
{
    public DeletePersonalVocabularyWordCommandValidator()
    {
        RuleFor(c => c.WordId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
    }
}
