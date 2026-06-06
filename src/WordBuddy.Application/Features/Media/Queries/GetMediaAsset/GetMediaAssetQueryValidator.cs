using FluentValidation;

namespace WordBuddy.Application.Features.Media.Queries.GetMediaAsset;

internal sealed class GetMediaAssetQueryValidator : AbstractValidator<GetMediaAssetQuery>
{
    public GetMediaAssetQueryValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
    }
}
