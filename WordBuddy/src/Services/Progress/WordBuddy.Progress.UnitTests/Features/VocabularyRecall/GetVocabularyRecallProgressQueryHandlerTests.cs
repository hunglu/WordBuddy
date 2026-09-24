using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularyRecall;

public class GetVocabularyRecallProgressQueryHandlerTests
{
    private readonly Mock<IVocabularyRecallRepository> _repository = new();
    private readonly Mock<ILogger<GetVocabularyRecallProgressQueryHandler>> _logger = new();

    private GetVocabularyRecallProgressQueryHandler CreateHandler() => new(_repository.Object, _logger.Object);

    private static VocabularyRecallStat CreateStat(Guid userId, bool known)
    {
        VocabularyRecallStat stat = new(Guid.NewGuid(), userId, Guid.NewGuid(), "apple");
        stat.ApplyCheckResult(known);
        return stat;
    }

    [Fact]
    public async Task HandleAsync_MixOfKnownAndLearningStats_ComputesCountsAndReturnsSessions()
    {
        Guid userId = Guid.NewGuid();
        List<VocabularyRecallStat> stats =
        [
            CreateStat(userId, true),
            CreateStat(userId, true),
            CreateStat(userId, false),
        ];
        List<VocabularyRecallSession> sessions = [new(Guid.NewGuid(), userId, 3, 2)];

        _repository
            .Setup(r => r.GetStatsByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyRecallStat>>(stats));
        _repository
            .Setup(r => r.GetRecentSessionsByUserAsync(userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyRecallSession>>(sessions));

        Result<VocabularyRecallProgressDto> result = await CreateHandler().HandleAsync(new GetVocabularyRecallProgressQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalWordsTracked.Should().Be(3);
        result.Value.KnownCount.Should().Be(2);
        result.Value.LearningCount.Should().Be(1);
        result.Value.RecentSessions.Should().ContainSingle(s => s.WordsChecked == 3 && s.WordsKnown == 2);
    }

    [Fact]
    public async Task HandleAsync_NoStatsYet_ReturnsZeroCounts()
    {
        Guid userId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetStatsByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyRecallStat>>([]));
        _repository
            .Setup(r => r.GetRecentSessionsByUserAsync(userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyRecallSession>>([]));

        Result<VocabularyRecallProgressDto> result = await CreateHandler().HandleAsync(new GetVocabularyRecallProgressQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalWordsTracked.Should().Be(0);
        result.Value.KnownCount.Should().Be(0);
        result.Value.LearningCount.Should().Be(0);
        result.Value.RecentSessions.Should().BeEmpty();
    }
}
