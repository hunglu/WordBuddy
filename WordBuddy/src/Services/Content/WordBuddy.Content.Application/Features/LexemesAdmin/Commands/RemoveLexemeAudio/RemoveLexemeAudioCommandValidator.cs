using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.RemoveLexemeAudio;

public sealed class RemoveLexemeAudioCommandValidator : AbstractValidator<RemoveLexemeAudioCommand>
{
    public RemoveLexemeAudioCommandValidator()
    {
        RuleFor(c => c.LexemeId).NotEmpty();
        RuleFor(c => c.Locale)
            .Must(l => l is Lexeme.UkLocale or Lexeme.UsLocale)
            .WithMessage($"Locale must be '{Lexeme.UkLocale}' or '{Lexeme.UsLocale}'.");
    }
}
