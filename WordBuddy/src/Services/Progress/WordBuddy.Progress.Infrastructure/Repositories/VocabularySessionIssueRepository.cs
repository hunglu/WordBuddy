using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

/// <summary>Insert-only: no update or delete methods exist.</summary>
internal sealed class VocabularySessionIssueRepository : IVocabularySessionIssueRepository
{
    private readonly ProgressDbContext _dbContext;

    public VocabularySessionIssueRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddAsync(VocabularySessionIssue issue, CancellationToken ct = default)
    {
        await _dbContext.VocabularySessionIssues.AddAsync(issue, ct);
        return Result.Success();
    }
}
