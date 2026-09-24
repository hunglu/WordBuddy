using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;

public sealed class RequestShareVocabularyWordCommandValidator : AbstractValidator<RequestShareVocabularyWordCommand>
{
    public RequestShareVocabularyWordCommandValidator()
    {
        RuleFor(c => c.WordId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
    }
}
