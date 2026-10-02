using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.VocabularyRemaps.Queries.GetPendingVocabularyWordIdRemaps;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.VocabularyRemaps;

public class GetPendingVocabularyWordIdRemapsQueryHandlerTests
{
    private readonly Mock<IVocabularyWordIdRemapRepository> _repository = new();
    private readonly GetPendingVocabularyWordIdRemapsQueryValidator _validator = new();
    private readonly Mock<ILogger<GetPendingVocabularyWordIdRemapsQueryHandler>> _logger = new();

    private GetPendingVocabularyWordIdRemapsQueryHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task GetPendingVocabularyWordIdRemapsQueryHandler_HandleAsync_ValidLimit_ReturnsRepositoryRemaps()
    {
        List<VocabularyWordIdRemapDto> remaps = [new(Guid.NewGuid(), Guid.NewGuid()), new(Guid.NewGuid(), Guid.NewGuid())];
        _repository
            .Setup(r => r.GetPendingAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyWordIdRemapDto>>(remaps));

        Result<IReadOnlyList<VocabularyWordIdRemapDto>> result = await CreateHandler().HandleAsync(new GetPendingVocabularyWordIdRemapsQuery(100));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(remaps);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task GetPendingVocabularyWordIdRemapsQueryHandler_HandleAsync_LimitOutOfRange_ReturnsValidationWithoutQuerying(int limit)
    {
        Result<IReadOnlyList<VocabularyWordIdRemapDto>> result = await CreateHandler().HandleAsync(new GetPendingVocabularyWordIdRemapsQuery(limit));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void GetPendingVocabularyWordIdRemapsQueryValidator_Validate_LimitAtBounds_IsValid(int limit)
    {
        _validator.Validate(new GetPendingVocabularyWordIdRemapsQuery(limit)).IsValid.Should().BeTrue();
    }
}
