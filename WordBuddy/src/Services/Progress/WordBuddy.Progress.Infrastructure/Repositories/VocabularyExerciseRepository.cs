using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

/// <summary>No delete method exists; <see cref="VocabularyExercise.Answer"/> is the only update.</summary>
internal sealed class VocabularyExerciseRepository : IVocabularyExerciseRepository
{
    private readonly ProgressDbContext _dbContext;

    public VocabularyExerciseRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddAsync(VocabularyExercise exercise, CancellationToken ct = default)
    {
        await _dbContext.VocabularyExercises.AddAsync(exercise, ct);
        return Result.Success();
    }

    public async Task<Result<VocabularyExercise>> GetTrackedAsync(Guid exerciseId, CancellationToken ct = default)
    {
        VocabularyExercise? exercise = await _dbContext.VocabularyExercises
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId, ct);

        return exercise is null
            ? Result.Failure<VocabularyExercise>(Error.NotFound("Exercise.NotFound", "The exercise was not found."))
            : Result.Success(exercise);
    }
}
