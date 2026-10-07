using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;

public sealed class RepublishLearnerWordsCommandValidator : AbstractValidator<RepublishLearnerWordsCommand>
{
    public RepublishLearnerWordsCommandValidator()
    {
        RuleFor(c => c.BatchSize).InclusiveBetween(1, 5000);
    }
}
