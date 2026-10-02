using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularyRecall;

public class RemapVocabularyWordIdsCommandHandlerTests
{
    private readonly Mock<IVocabularyRecallRepository> _repository = new();
    private readonly RemapVocabularyWordIdsCommandValidator _validator = new();
    private readonly Mock<ILogger<RemapVocabularyWordIdsCommandHandler>> _logger = new();
    private readonly List<VocabularyRecallStat> _removed = [];

    public RemapVocabularyWordIdsCommandHandlerTests()
    {
        _repository
            .Setup(r => r.SaveRemappedStatsAsync(It.IsAny<IReadOnlyList<VocabularyRecallStat>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<VocabularyRecallStat>, CancellationToken>((removed, _) => _removed.AddRange(removed))
            .ReturnsAsync(Result.Success());
    }

    private RemapVocabularyWordIdsCommandHandler CreateHandler() => new(_repository.Object, _validator, _logger.Object);

    private void SetupStats(params VocabularyRecallStat[] stats) =>
        _repository
            .Setup(r => r.GetTrackedStatsByWordIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyRecallStat>>(stats));

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
    public async Task RemapVocabularyWordIdsCommandHandler_HandleAsync_NoStatForNewId_RewritesInPlace()
    {
        Guid userId = Guid.NewGuid();
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        VocabularyRecallStat stat = Stat(userId, oldId, true, false);
        SetupStats(stat);

        Result result = await CreateHandler().HandleAsync(new RemapVocabularyWordIdsCommand([new(oldId, newId)]));

        result.IsSuccess.Should().BeTrue();
        stat.VocabularyWordId.Should().Be(newId);
        stat.TimesChecked.Should().Be(2);
        _removed.Should().BeEmpty();
    }

    [Fact]
    public async Task RemapVocabularyWordIdsCommandHandler_HandleAsync_StatForNewIdExists_MergesCountsAndRemovesOld()
    {
        Guid userId = Guid.NewGuid();
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        VocabularyRecallStat target = Stat(userId, newId, true);
        VocabularyRecallStat old = Stat(userId, oldId, true, false, true);
        SetupStats(target, old);

        Result result = await CreateHandler().HandleAsync(new RemapVocabularyWordIdsCommand([new(oldId, newId)]));

        result.IsSuccess.Should().BeTrue();
        target.TimesChecked.Should().Be(4);
        target.TimesKnown.Should().Be(3);
        target.VocabularyWordId.Should().Be(newId);
        _removed.Should().ContainSingle().Which.Should().BeSameAs(old);
    }

    [Fact]
    public async Task RemapVocabularyWordIdsCommandHandler_HandleAsync_OtherUsersStatForNewId_DoesNotMergeAcrossUsers()
    {
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        VocabularyRecallStat otherUsersTarget = Stat(Guid.NewGuid(), newId, true);
        VocabularyRecallStat mine = Stat(Guid.NewGuid(), oldId, false);
        SetupStats(otherUsersTarget, mine);

        await CreateHandler().HandleAsync(new RemapVocabularyWordIdsCommand([new(oldId, newId)]));

        mine.VocabularyWordId.Should().Be(newId);
        otherUsersTarget.TimesChecked.Should().Be(1);
        _removed.Should().BeEmpty();
    }

    [Fact]
    public async Task RemapVocabularyWordIdsCommandHandler_HandleAsync_TwoOldIdsIntoOneNew_MergesSecondIntoFirst()
    {
        Guid userId = Guid.NewGuid();
        Guid old1 = Guid.NewGuid();
        Guid old2 = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        VocabularyRecallStat first = Stat(userId, old1, true);
        VocabularyRecallStat second = Stat(userId, old2, false, false);
        SetupStats(first, second);

        await CreateHandler().HandleAsync(new RemapVocabularyWordIdsCommand([new(old1, newId), new(old2, newId)]));

        first.VocabularyWordId.Should().Be(newId);
        first.TimesChecked.Should().Be(3);
        _removed.Should().ContainSingle().Which.Should().BeSameAs(second);
    }

    [Fact]
    public async Task RemapVocabularyWordIdsCommandHandler_HandleAsync_ReRunAfterRemap_ChangesNothing()
    {
        Guid userId = Guid.NewGuid();
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        // After the first run only the new-id stat exists.
        VocabularyRecallStat target = Stat(userId, newId, true, true);
        SetupStats(target);

        Result result = await CreateHandler().HandleAsync(new RemapVocabularyWordIdsCommand([new(oldId, newId)]));

        result.IsSuccess.Should().BeTrue();
        target.TimesChecked.Should().Be(2);
        target.VocabularyWordId.Should().Be(newId);
        _removed.Should().BeEmpty();
    }

    [Fact]
    public async Task RemapVocabularyWordIdsCommandHandler_HandleAsync_SameOldAndNewId_ReturnsValidationFailure()
    {
        Guid id = Guid.NewGuid();

        Result result = await CreateHandler().HandleAsync(new RemapVocabularyWordIdsCommand([new(id, id)]));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.GetTrackedStatsByWordIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
