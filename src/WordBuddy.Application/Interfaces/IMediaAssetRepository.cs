using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.Interfaces;

/// <summary>Repository contract for <see cref="MediaAsset"/> entities.</summary>
public interface IMediaAssetRepository : IRepository<MediaAsset>
{
    /// <summary>Retrieves all media assets of the specified format type.</summary>
    Task<Result<IReadOnlyList<MediaAsset>>> GetByTypeAsync(MediaAssetType type, CancellationToken ct = default);
}
