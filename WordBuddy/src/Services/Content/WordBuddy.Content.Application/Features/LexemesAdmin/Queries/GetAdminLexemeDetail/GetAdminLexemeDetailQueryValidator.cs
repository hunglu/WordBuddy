using FluentValidation;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemeDetail;

public sealed class GetAdminLexemeDetailQueryValidator : AbstractValidator<GetAdminLexemeDetailQuery>
{
    public GetAdminLexemeDetailQueryValidator()
    {
        RuleFor(q => q.LexemeId).NotEmpty();
    }
}
