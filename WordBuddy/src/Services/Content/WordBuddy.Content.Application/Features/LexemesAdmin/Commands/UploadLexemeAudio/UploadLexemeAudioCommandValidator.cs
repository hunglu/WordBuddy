using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UploadLexemeAudio;

public sealed class UploadLexemeAudioCommandValidator : AbstractValidator<UploadLexemeAudioCommand>
{
    public UploadLexemeAudioCommandValidator()
    {
        RuleFor(c => c.LexemeId).NotEmpty();
        RuleFor(c => c.Locale)
            .Must(l => l is Lexeme.UkLocale or Lexeme.UsLocale)
            .WithMessage($"Locale must be '{Lexeme.UkLocale}' or '{Lexeme.UsLocale}'.");
        RuleFor(c => c.ContentType)
            .Must(t => string.Equals(t, UploadLexemeAudioCommand.AllowedContentType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, UploadLexemeAudioCommand.AliasContentType, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"Content type must be {UploadLexemeAudioCommand.AllowedContentType}.");
        RuleFor(c => c.Length)
            .InclusiveBetween(1, UploadLexemeAudioCommand.MaxBytes)
            .WithMessage("The file must be between 1 byte and 5 MB.");
    }
}
