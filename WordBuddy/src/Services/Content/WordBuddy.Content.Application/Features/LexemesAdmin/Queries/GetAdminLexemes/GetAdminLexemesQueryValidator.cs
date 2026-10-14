using FluentValidation;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemes;

public sealed class GetAdminLexemesQueryValidator : AbstractValidator<GetAdminLexemesQuery>
{
    public GetAdminLexemesQueryValidator()
    {
        RuleFor(q => q.Search).MaximumLength(Lexeme.LemmaMaxLength);
        RuleFor(q => q.PartOfSpeech).IsInEnum();
        RuleFor(q => q.CefrLevel).IsInEnum();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, GetAdminLexemesQuery.MaxPageSize);
    }
}
