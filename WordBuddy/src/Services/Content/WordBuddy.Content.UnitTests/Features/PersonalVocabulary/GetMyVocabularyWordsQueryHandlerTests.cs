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

    private GetMyVocabularyWordsQueryHandler CreateHandler() => new(_repository.Object, _logger.Object);

    private async Task<PersonalVocabularyWordDto> GetSingleAsync(Guid userId, LearnerWord link)
    {
        _repository
            .Setup(r => r.GetLinkedToUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LearnerWord>>([link]));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetMyVocabularyWordsQuery(userId));

        result.IsSuccess.Should().BeTrue();
        return result.Value.Should().ContainSingle().Subject;
    }

    [Fact]
    public async Task GetMyVocabularyWordsQueryHandler_HandleAsync_AuthoredWord_ReportsWordsOwnValues()
    {
        Guid ownerId = Guid.NewGuid();
        Sense word = TestWords.Pending(ownerId, "apple", "a fruit", "eg");

        PersonalVocabularyWordDto dto = await GetSingleAsync(ownerId, new LearnerWord(Guid.NewGuid(), ownerId, word, isAuthor: true));

        dto.Should().BeEquivalentTo(new PersonalVocabularyWordDto(
            word.Id, ownerId, "apple", "a fruit", "eg", VocabularyShareStatus.PendingReview, false, word.CreatedAtUtc, IsAuthor: true));
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
}
