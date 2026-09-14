using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

public interface IMediaAssetRepository
{
    Task<Result<MediaAsset>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result> AddAsync(MediaAsset asset, CancellationToken ct = default);
}
