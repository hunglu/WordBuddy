using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class VocabularyLearnerSettingsRepository : IVocabularyLearnerSettingsRepository
{
    private readonly ProgressDbContext _dbContext;

    public VocabularyLearnerSettingsRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<VocabularyLearnerSettings>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        VocabularyLearnerSettings? settings = await _dbContext.VocabularyLearnerSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        return settings is null
            ? Result.Failure<VocabularyLearnerSettings>(Error.NotFound("VocabularySettings.NotFound", "No settings saved."))
            : Result.Success(settings);
    }

    public async Task<Result<VocabularyLearnerSettings>> UpsertAsync(Guid userId, int? newWordsPerDay, CancellationToken ct = default)
    {
        VocabularyLearnerSettings? settings = await _dbContext.VocabularyLearnerSettings
            .AsTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (settings is null)
        {
            Result<VocabularyLearnerSettings> created = VocabularyLearnerSettings.Create(userId, newWordsPerDay);
            if (created.IsFailure)
            {
                return created;
            }

            settings = created.Value;
            await _dbContext.VocabularyLearnerSettings.AddAsync(settings, ct);
        }
        else
        {
            Result updated = settings.SetNewWordsPerDay(newWordsPerDay);
            if (updated.IsFailure)
            {
                return Result.Failure<VocabularyLearnerSettings>(updated.Error);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success(settings);
    }
}
