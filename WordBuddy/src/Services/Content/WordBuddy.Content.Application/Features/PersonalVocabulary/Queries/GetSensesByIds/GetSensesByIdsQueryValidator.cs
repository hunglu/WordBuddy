using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;

/// <summary>1–100 ids, none empty.</summary>
public sealed class GetSensesByIdsQueryValidator : AbstractValidator<GetSensesByIdsQuery>
{
    /// <summary>Maximum number of ids per call.</summary>
    public const int MaxIds = 100;

    public GetSensesByIdsQueryValidator()
    {
        RuleFor(q => q.RequestingUserId).NotEmpty();
        RuleFor(q => q.RequestingAgeGroup).IsInEnum();
        RuleFor(q => q.Ids).NotEmpty();
        RuleFor(q => q.Ids.Count).LessThanOrEqualTo(MaxIds).When(q => q.Ids is not null);
        RuleForEach(q => q.Ids).NotEqual(Guid.Empty);
    }
}
