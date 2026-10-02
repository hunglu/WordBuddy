using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class VocabularyWordIdRemapRepository : IVocabularyWordIdRemapRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<VocabularyWordIdRemapRepository> _logger;

    public VocabularyWordIdRemapRepository(ContentDbContext dbContext, ILogger<VocabularyWordIdRemapRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<VocabularyWordIdRemapDto>>> GetPendingAsync(int limit, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying pending VocabularyWordIdRemaps: Limit={Limit}", limit);

        List<VocabularyWordIdRemapDto> remaps = await _dbContext.VocabularyWordIdRemaps
            .AsNoTracking()
            .Where(r => r.PublishedAtUtc == null)
            .OrderBy(r => r.OldId)
            .Take(limit)
            .Select(r => new VocabularyWordIdRemapDto(r.OldId, r.NewId))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VocabularyWordIdRemapDto>>(remaps);
    }

    public async Task<Result<int>> AcknowledgeAsync(IReadOnlyCollection<Guid> oldIds, CancellationToken ct = default)
    {
        _logger.LogDebug("Acknowledging VocabularyWordIdRemaps: Count={Count}", oldIds.Count);

        DateTime acknowledgedAtUtc = DateTime.UtcNow;
        List<Guid> ids = oldIds.Distinct().ToList();

        int stamped = await _dbContext.VocabularyWordIdRemaps
            .Where(r => ids.Contains(r.OldId) && r.PublishedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.PublishedAtUtc, acknowledgedAtUtc), ct);

        return Result.Success(stamped);
    }
}
