using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SyncVocabularyWordIdRemaps;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularyRecall;

public class SyncVocabularyWordIdRemapsCommandHandlerTests
{
    private readonly Mock<IContentVocabularyRemapClient> _client = new();
    private readonly Mock<ICommandHandler<RemapVocabularyWordIdsCommand>> _remap = new();
    private readonly SyncVocabularyWordIdRemapsCommandValidator _validator = new();
    private readonly Mock<ILogger<SyncVocabularyWordIdRemapsCommandHandler>> _logger = new();

    public SyncVocabularyWordIdRemapsCommandHandlerTests()
    {
        _remap
            .Setup(h => h.HandleAsync(It.IsAny<RemapVocabularyWordIdsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _client
            .Setup(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private SyncVocabularyWordIdRemapsCommandHandler CreateHandler() =>
        new(_client.Object, _remap.Object, _validator, _logger.Object);

    private static List<VocabularyWordIdRemapPair> Batch(int count) =>
        Enumerable.Range(0, count).Select(_ => new VocabularyWordIdRemapPair(Guid.NewGuid(), Guid.NewGuid())).ToList();

    private void SetupPending(params List<VocabularyWordIdRemapPair>[] batches)
    {
        Moq.Language.ISetupSequentialResult<Task<Result<IReadOnlyList<VocabularyWordIdRemapPair>>>> sequence =
            _client.SetupSequence(c => c.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()));
        foreach (List<VocabularyWordIdRemapPair> batch in batches)
        {
            sequence = sequence.ReturnsAsync(Result.Success<IReadOnlyList<VocabularyWordIdRemapPair>>(batch));
        }
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_NothingPending_DoesNotRemapOrAcknowledge()
    {
        SetupPending(Batch(0));

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        _remap.Verify(h => h.HandleAsync(It.IsAny<RemapVocabularyWordIdsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _client.Verify(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_OnePartialBatch_RemapsThenAcknowledgesAndStops()
    {
        List<VocabularyWordIdRemapPair> batch = Batch(3);
        SetupPending(batch);
        List<string> calls = [];
        _remap
            .Setup(h => h.HandleAsync(It.IsAny<RemapVocabularyWordIdsCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("remap"))
            .ReturnsAsync(Result.Success());
        _client
            .Setup(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("ack"))
            .ReturnsAsync(Result.Success());

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
        calls.Should().Equal("remap", "ack");
        _remap.Verify(h => h.HandleAsync(
            It.Is<RemapVocabularyWordIdsCommand>(c => c.Remaps.Select(r => r.OldId).SequenceEqual(batch.Select(p => p.OldId))
                && c.Remaps.Select(r => r.NewId).SequenceEqual(batch.Select(p => p.NewId))),
            It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.AcknowledgeAsync(
            It.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(batch.Select(p => p.OldId))),
            It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_FullBatch_LoopsUntilBatchIsNotFull()
    {
        SetupPending(Batch(2), Batch(2), Batch(1));

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(2));

        result.Value.Should().Be(5);
        _client.Verify(c => c.GetPendingAsync(2, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _client.Verify(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_FullBatchThenEmpty_StopsOnEmpty()
    {
        SetupPending(Batch(2), []);

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(2));

        result.Value.Should().Be(2);
        _client.Verify(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_RemapFails_DoesNotAcknowledgeAndReturnsFailure()
    {
        SetupPending(Batch(2));
        Error error = Error.Failure("VocabularyRecall.SaveFailed", "boom");
        _remap
            .Setup(h => h.HandleAsync(It.IsAny<RemapVocabularyWordIdsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        _client.Verify(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_ClientFetchFails_ReturnsFailureWithoutRemapping()
    {
        Error error = ContentApiErrors.Unavailable("down");
        _client
            .Setup(c => c.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<VocabularyWordIdRemapPair>>(error));

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContentApi.Unavailable");
        _remap.Verify(h => h.HandleAsync(It.IsAny<RemapVocabularyWordIdsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_AcknowledgeFails_ReturnsFailure()
    {
        SetupPending(Batch(2));
        _client
            .Setup(c => c.AcknowledgeAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ContentApiErrors.Unauthorized("401")));

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContentApi.Unauthorized");
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_AlwaysFullBatches_StopsAt50Batches()
    {
        _client
            .Setup(c => c.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result.Success<IReadOnlyList<VocabularyWordIdRemapPair>>(Batch(1)));

        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(SyncVocabularyWordIdRemapsCommandHandler.MaxBatchesPerRun);
        _client.Verify(c => c.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(50));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task SyncVocabularyWordIdRemapsCommandHandler_HandleAsync_BatchSizeOutOfRange_ReturnsValidation(int batchSize)
    {
        Result<int> result = await CreateHandler().HandleAsync(new SyncVocabularyWordIdRemapsCommand(batchSize));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _client.Verify(c => c.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
