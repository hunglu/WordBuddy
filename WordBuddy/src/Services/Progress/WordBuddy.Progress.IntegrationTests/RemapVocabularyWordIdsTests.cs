using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// <see cref="RemapVocabularyWordIdsCommand"/> against a real SQL Server database (unique
/// <c>(UserId, VocabularyWordId)</c> index included), resolved from the API host's own DI container.
/// This calls the command handler directly; pulling remaps from Content is covered by
/// <see cref="SyncVocabularyWordIdRemapsTests"/>.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class RemapVocabularyWordIdsTests
{
    private readonly ProgressApiFactory _factory;

    public RemapVocabularyWordIdsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAsync(params VocabularyRecallStat[] stats)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        await dbContext.VocabularyRecallStats.AddRangeAsync(stats);
        await dbContext.SaveChangesAsync();
    }

    private async Task<Result> RemapAsync(params VocabularyWordIdRemap[] remaps)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ICommandHandler<RemapVocabularyWordIdsCommand> handler =
            scope.ServiceProvider.GetRequiredService<ICommandHandler<RemapVocabularyWordIdsCommand>>();
        return await handler.HandleAsync(new RemapVocabularyWordIdsCommand(remaps));
    }

    private async Task<List<VocabularyRecallStat>> GetStatsAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await dbContext.VocabularyRecallStats.AsNoTracking().Where(s => s.UserId == userId).ToListAsync();
    }

    private static VocabularyRecallStat Stat(Guid userId, Guid wordId, params bool[] checks)
    {
        VocabularyRecallStat stat = new(Guid.NewGuid(), userId, wordId, "apple");
        foreach (bool known in checks)
        {
            stat.ApplyCheckResult(known);
        }
        return stat;
    }

    [Fact]
    public async Task RemapVocabularyWordIds_RewritesOrMergesAndIsIdempotent()
    {
        Guid rewriteUser = Guid.NewGuid();
        Guid mergeUser = Guid.NewGuid();
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();

        await SeedAsync(
            Stat(rewriteUser, oldId, true, false),
            Stat(mergeUser, newId, true),
            Stat(mergeUser, oldId, false, true));

        (await RemapAsync(new VocabularyWordIdRemap(oldId, newId))).IsSuccess.Should().BeTrue();
        (await RemapAsync(new VocabularyWordIdRemap(oldId, newId))).IsSuccess.Should().BeTrue();

        List<VocabularyRecallStat> rewritten = await GetStatsAsync(rewriteUser);
        rewritten.Should().ContainSingle().Which.Should().Match<VocabularyRecallStat>(s =>
            s.VocabularyWordId == newId && s.TimesChecked == 2 && s.TimesKnown == 1);

        List<VocabularyRecallStat> merged = await GetStatsAsync(mergeUser);
        merged.Should().ContainSingle().Which.Should().Match<VocabularyRecallStat>(s =>
            s.VocabularyWordId == newId && s.TimesChecked == 3 && s.TimesKnown == 2);
    }
}
