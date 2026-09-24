using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class GetRandomVocabularyWordsForCheckQueryHandlerTests
{
    private readonly Mock<IPersonalVocabularyWordRepository> _repository = new();
    private readonly GetRandomVocabularyWordsForCheckQueryValidator _validator = new();
    private readonly Mock<ILogger<GetRandomVocabularyWordsForCheckQueryHandler>> _logger = new();

    private GetRandomVocabularyWordsForCheckQueryHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task HandleAsync_ValidCount_ReturnsAtMostCountItemsFromOwnersList()
    {
        Guid ownerId = Guid.NewGuid();
        List<PersonalVocabularyWord> words =
        [
            new(Guid.NewGuid(), ownerId, AgeGroup.Adult, "apple", "a fruit", null),
            new(Guid.NewGuid(), ownerId, AgeGroup.Adult, "banana", "a fruit", null),
        ];

        _repository
            .Setup(r => r.GetRandomByOwnerAsync(ownerId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<PersonalVocabularyWord>>(words));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetRandomVocabularyWordsForCheckQuery(ownerId, 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(dto => dto.OwnerUserId == ownerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task HandleAsync_CountOutOfRange_ReturnsValidationFailure(int count)
    {
        Result<IReadOnlyList<PersonalVocabularyWordDto>> result =
            await CreateHandler().HandleAsync(new GetRandomVocabularyWordsForCheckQuery(Guid.NewGuid(), count));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.GetRandomByOwnerAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
