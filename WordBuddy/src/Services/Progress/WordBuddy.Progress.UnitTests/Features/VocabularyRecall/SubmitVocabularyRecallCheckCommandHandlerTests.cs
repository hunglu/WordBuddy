using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularyRecall;

public class SubmitVocabularyRecallCheckCommandHandlerTests
{
    private readonly Mock<IVocabularyRecallRepository> _repository = new();
    private readonly SubmitVocabularyRecallCheckCommandValidator _validator = new();
    private readonly Mock<ILogger<SubmitVocabularyRecallCheckCommandHandler>> _logger = new();

    public SubmitVocabularyRecallCheckCommandHandlerTests()
    {
        _repository
            .Setup(r => r.UpsertStatsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<VocabularyRecallResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _repository
            .Setup(r => r.AddSessionAsync(It.IsAny<VocabularyRecallSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private SubmitVocabularyRecallCheckCommandHandler CreateHandler() => new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task HandleAsync_NewWord_UpsertsStatAndAddsSession()
    {
        Guid userId = Guid.NewGuid();
        SubmitVocabularyRecallCheckCommand command = new(
            userId,
            [new VocabularyRecallResultItem(Guid.NewGuid(), "apple", true)]);

        Result result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.UpsertStatsAsync(userId, It.Is<IReadOnlyList<VocabularyRecallResult>>(l => l.Count == 1 && l[0].Word == "apple"), It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(
            r => r.AddSessionAsync(It.Is<VocabularyRecallSession>(s => s.UserId == userId && s.WordsChecked == 1 && s.WordsKnown == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingWord_UpsertsStatsForAllResultsAndSummarizesSession()
    {
        Guid userId = Guid.NewGuid();
        SubmitVocabularyRecallCheckCommand command = new(
            userId,
            [
                new VocabularyRecallResultItem(Guid.NewGuid(), "apple", true),
                new VocabularyRecallResultItem(Guid.NewGuid(), "banana", false),
            ]);

        Result result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.UpsertStatsAsync(userId, It.Is<IReadOnlyList<VocabularyRecallResult>>(l => l.Count == 2), It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(
            r => r.AddSessionAsync(It.Is<VocabularyRecallSession>(s => s.WordsChecked == 2 && s.WordsKnown == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyResults_ReturnsValidationFailureWithoutCallingRepository()
    {
        SubmitVocabularyRecallCheckCommand command = new(Guid.NewGuid(), []);

        Result result = await CreateHandler().HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.UpsertStatsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<VocabularyRecallResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OverfiftyResults_ReturnsValidationFailure()
    {
        List<VocabularyRecallResultItem> results = Enumerable.Range(0, 51)
            .Select(i => new VocabularyRecallResultItem(Guid.NewGuid(), $"word{i}", true))
            .ToList();

        SubmitVocabularyRecallCheckCommand command = new(Guid.NewGuid(), results);

        Result result = await CreateHandler().HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
