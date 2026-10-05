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
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly GetRandomVocabularyWordsForCheckQueryValidator _validator = new();
    private readonly Mock<ILogger<GetRandomVocabularyWordsForCheckQueryHandler>> _logger = new();

    private GetRandomVocabularyWordsForCheckQueryHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task GetRandomVocabularyWordsForCheckQueryHandler_HandleAsync_ValidCount_ReturnsLinkedWordsAsRequesterOwned()
    {
        Guid ownerId = Guid.NewGuid();
        Sense own = TestWords.Learner(ownerId, "apple");
        Sense adopted = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, "banana");
        List<LearnerWord> links =
        [
            new(Guid.NewGuid(), ownerId, own, isAuthor: true),
            new(Guid.NewGuid(), ownerId, adopted, isAuthor: false),
        ];

        _repository
            .Setup(r => r.GetRandomLinkedToUserAsync(ownerId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LearnerWord>>(links));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetRandomVocabularyWordsForCheckQuery(ownerId, 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(dto => dto.OwnerUserId == ownerId);
        result.Value.Should().ContainSingle(dto => dto.Id == own.Id && dto.IsAuthor);
        result.Value.Should().ContainSingle(dto => dto.Id == adopted.Id && !dto.IsAuthor && dto.ShareStatus == VocabularyShareStatus.Private);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task GetRandomVocabularyWordsForCheckQueryHandler_HandleAsync_CountOutOfRange_ReturnsValidationFailure(int count)
    {
        Result<IReadOnlyList<PersonalVocabularyWordDto>> result =
            await CreateHandler().HandleAsync(new GetRandomVocabularyWordsForCheckQuery(Guid.NewGuid(), count));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.GetRandomLinkedToUserAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
