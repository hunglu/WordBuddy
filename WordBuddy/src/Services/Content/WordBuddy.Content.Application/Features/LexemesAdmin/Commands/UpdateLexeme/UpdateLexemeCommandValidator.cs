using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UpdateLexeme;

public sealed class UpdateLexemeCommandValidator : AbstractValidator<UpdateLexemeCommand>
{
    public UpdateLexemeCommandValidator()
    {
        RuleFor(c => c.LexemeId).NotEmpty();
        RuleFor(c => c.PartOfSpeech).IsInEnum();
        RuleFor(c => c.IpaUk).MaximumLength(Lexeme.IpaMaxLength);
        RuleFor(c => c.IpaUs).MaximumLength(Lexeme.IpaMaxLength);
        RuleFor(c => c.Syllables).MaximumLength(Lexeme.SyllablesMaxLength);
        RuleFor(c => c.CefrLevel).IsInEnum();
        RuleFor(c => c.FrequencyRank).InclusiveBetween(1, Lexeme.FrequencyRankMax);
        RuleFor(c => c.WordForms)
            .Must(forms => forms is null || forms.Count <= Lexeme.MaxWordForms)
            .WithMessage($"At most {Lexeme.MaxWordForms} word forms are allowed.");
        RuleForEach(c => c.WordForms).MaximumLength(Lexeme.WordFormMaxLength);
    }
}
