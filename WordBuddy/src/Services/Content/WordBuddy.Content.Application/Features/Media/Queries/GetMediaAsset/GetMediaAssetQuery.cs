using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.Media.Queries.GetMediaAsset;

public sealed record GetMediaAssetQuery(Guid MediaAssetId) : IQuery<MediaAssetDto>;
