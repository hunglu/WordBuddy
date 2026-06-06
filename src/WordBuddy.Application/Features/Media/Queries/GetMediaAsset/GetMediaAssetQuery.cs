namespace WordBuddy.Application.Features.Media.Queries.GetMediaAsset;

/// <summary>Query to retrieve a <see cref="WordBuddy.Domain.Entities.MediaAsset"/> by its identifier.</summary>
public sealed record GetMediaAssetQuery(Guid AssetId);
