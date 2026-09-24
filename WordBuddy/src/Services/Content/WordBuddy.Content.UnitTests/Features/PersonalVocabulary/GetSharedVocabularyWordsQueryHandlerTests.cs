using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class GetSharedVocabularyWordsQueryHandlerTests
{
    private readonly Mock<IPersonalVocabularyWordRepository> _repository = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<ILogger<GetSharedVocabularyWordsQueryHandler>> _logger = new();

    public GetSharedVocabularyWordsQueryHandlerTests()
    {
        // Always a cache miss so the handler always falls through to the repository.
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        _cache
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private GetSharedVocabularyWordsQueryHandler CreateHandler() => new(_repository.Object, _cache.Object, _logger.Object);

    private static PersonalVocabularyWord CreateSharedWord(bool visibleToChildren)
    {
        PersonalVocabularyWord word = new(Guid.NewGuid(), Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null);
        word.RequestShare();
        word.Approve(visibleToChildren, Guid.NewGuid());
        return word;
    }

    [Fact]
    public async Task HandleAsync_AdultCaller_ReceivesAllSharedItems()
    {
        _repository
            .Setup(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<PersonalVocabularyWord>>([CreateSharedWord(true), CreateSharedWord(false)]));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _repository.Verify(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ChildCaller_OnlyReceivesVisibleToChildrenItems()
    {
        _repository
            .Setup(r => r.GetSharedAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<PersonalVocabularyWord>>([CreateSharedWord(true)]));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Child));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(dto => dto.VisibleToChildren);
        _repository.Verify(r => r.GetSharedAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
