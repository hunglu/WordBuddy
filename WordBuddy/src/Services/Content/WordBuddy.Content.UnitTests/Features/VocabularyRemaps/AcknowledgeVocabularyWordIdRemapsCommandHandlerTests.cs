using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.VocabularyRemaps.Commands.AcknowledgeVocabularyWordIdRemaps;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.VocabularyRemaps;

public class AcknowledgeVocabularyWordIdRemapsCommandHandlerTests
{
    private readonly Mock<IVocabularyWordIdRemapRepository> _repository = new();
    private readonly AcknowledgeVocabularyWordIdRemapsCommandValidator _validator = new();
    private readonly Mock<ILogger<AcknowledgeVocabularyWordIdRemapsCommandHandler>> _logger = new();

    private AcknowledgeVocabularyWordIdRemapsCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task AcknowledgeVocabularyWordIdRemapsCommandHandler_HandleAsync_ValidIds_PassesIdsThroughToRepository()
    {
        List<Guid> ids = [Guid.NewGuid(), Guid.NewGuid()];
        _repository
            .Setup(r => r.AcknowledgeAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(2));

        Result result = await CreateHandler().HandleAsync(new AcknowledgeVocabularyWordIdRemapsCommand(ids));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.AcknowledgeAsync(It.Is<IReadOnlyCollection<Guid>>(c => c.SequenceEqual(ids)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcknowledgeVocabularyWordIdRemapsCommandHandler_HandleAsync_NothingStamped_StillSucceeds()
    {
        _repository
            .Setup(r => r.AcknowledgeAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(0));

        Result result = await CreateHandler().HandleAsync(new AcknowledgeVocabularyWordIdRemapsCommand([Guid.NewGuid()]));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AcknowledgeVocabularyWordIdRemapsCommandHandler_HandleAsync_EmptyList_ReturnsValidationWithoutUpdating()
    {
        Result result = await CreateHandler().HandleAsync(new AcknowledgeVocabularyWordIdRemapsCommand([]));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.AcknowledgeAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcknowledgeVocabularyWordIdRemapsCommandHandler_HandleAsync_MoreThan500Ids_ReturnsValidationWithoutUpdating()
    {
        List<Guid> ids = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToList();

        Result result = await CreateHandler().HandleAsync(new AcknowledgeVocabularyWordIdRemapsCommand(ids));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.AcknowledgeAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcknowledgeVocabularyWordIdRemapsCommandHandler_HandleAsync_EmptyGuid_ReturnsValidationWithoutUpdating()
    {
        Result result = await CreateHandler().HandleAsync(new AcknowledgeVocabularyWordIdRemapsCommand([Guid.NewGuid(), Guid.Empty]));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.AcknowledgeAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void AcknowledgeVocabularyWordIdRemapsCommandValidator_Validate_Exactly500Ids_IsValid()
    {
        List<Guid> ids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();

        _validator.Validate(new AcknowledgeVocabularyWordIdRemapsCommand(ids)).IsValid.Should().BeTrue();
    }
}
