using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class GetMyVocabularyWordsQueryHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly Mock<ILogger<GetMyVocabularyWordsQueryHandler>> _logger = new();

    private GetMyVocabularyWordsQueryHandler CreateHandler() => new(_repository.Object, new GetMyVocabularyWordsQueryValidator(), _logger.Object);

    private async Task<PersonalVocabularyWordDto> GetSingleAsync(Guid userId, LearnerWord link)
    {
        _repository
            .Setup(r => r.GetLinkedToUserAsync(userId, 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PagedResult<LearnerWord>([link], 1, 25, 1)));

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetMyVocabularyWordsQuery(userId, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        return result.Value.Items.Should().ContainSingle().Subject;
    }

    [Fact]
    public async Task GetMyVocabularyWordsQueryHandler_HandleAsync_AuthoredWord_ReportsWordsOwnValues()
    {
        Guid ownerId = Guid.NewGuid();
        Sense word = TestWords.Pending(ownerId, "apple", "a fruit", "eg");

        PersonalVocabularyWordDto dto = await GetSingleAsync(ownerId, new LearnerWord(Guid.NewGuid(), ownerId, word, isAuthor: true));

        dto.Should().BeEquivalentTo(new PersonalVocabularyWordDto(
            word.Id, ownerId, "apple", "a fruit", "eg", VocabularyShareStatus.PendingReview, false, word.CreatedAtUtc, IsAuthor: true,
            Examples: ["eg"], Translations: [], Collocations: [], Synonyms: [], Antonyms: [], TopicTags: []));
    }

    [Fact]
    public async Task GetMyVocabularyWordsQueryHandler_HandleAsync_AdoptedSharedWord_ReportsLegacyCopyValues()
    {
        Guid adopterId = Guid.NewGuid();
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        LearnerWord link = new(Guid.NewGuid(), adopterId, shared, isAuthor: false);

        PersonalVocabularyWordDto dto = await GetSingleAsync(adopterId, link);

        dto.Id.Should().Be(shared.Id);
        dto.OwnerUserId.Should().Be(adopterId);
        dto.ShareStatus.Should().Be(VocabularyShareStatus.Private);
        dto.VisibleToChildren.Should().BeFalse();
        dto.CreatedAtUtc.Should().Be(link.AddedAtUtc);
        dto.IsAuthor.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyVocabularyWordsQueryHandler_HandleAsync_LinkedSystemWord_ReportsRequesterAsOwnerAndNotAuthor()
    {
        Guid userId = Guid.NewGuid();
        Sense system = TestWords.System();

        PersonalVocabularyWordDto dto = await GetSingleAsync(userId, new LearnerWord(Guid.NewGuid(), userId, system, isAuthor: false));

        dto.OwnerUserId.Should().Be(userId);
        dto.ShareStatus.Should().Be(VocabularyShareStatus.Private);
        dto.IsAuthor.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyVocabularyWordsQueryHandler_HandleAsync_PassesPagingAndKeepsTotal()
    {
        Guid userId = Guid.NewGuid();
        Sense system = TestWords.System();
        _repository
            .Setup(r => r.GetLinkedToUserAsync(userId, 3, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PagedResult<LearnerWord>([new LearnerWord(Guid.NewGuid(), userId, system, isAuthor: false)], 3, 5, 11)));

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetMyVocabularyWordsQuery(userId, AgeGroup.Child, 3, 5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(3);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalCount.Should().Be(11);
        result.Value.Items.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetMyVocabularyWordsQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetMyVocabularyWordsQuery(Guid.NewGuid(), AgeGroup.Adult, page, pageSize));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.VerifyNoOtherCalls();
    }
}
