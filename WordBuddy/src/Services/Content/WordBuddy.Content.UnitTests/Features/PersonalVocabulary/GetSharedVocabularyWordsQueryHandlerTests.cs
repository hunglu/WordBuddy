using System.Text;
using System.Text.Json;
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
    private readonly Mock<IVocabularyWordRepository> _repository = new();
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

    private static Sense CreateSharedWord(bool visibleToChildren) => TestWords.Shared(Guid.NewGuid(), visibleToChildren);

    [Fact]
    public async Task HandleAsync_AdultCaller_ReceivesAllSharedItems()
    {
        _repository
            .Setup(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([CreateSharedWord(true), CreateSharedWord(false)]));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _repository.Verify(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ChildCaller_OnlyReceivesVisibleToChildrenItems()
    {
        _repository
            .Setup(r => r.GetSharedAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([CreateSharedWord(true)]));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Child, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(dto => dto.VisibleToChildren);
        _repository.Verify(r => r.GetSharedAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_SetsIsMineForCaller()
    {
        Guid callerId = Guid.NewGuid();
        Sense own = TestWords.Shared(callerId, visibleToChildren: true);
        Sense other = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        Sense transferred = TestWords.Transferred(visibleToChildren: true);
        _repository
            .Setup(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([own, other, transferred]));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult, callerId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Single(d => d.Id == own.Id).IsMine.Should().BeTrue();
        result.Value.Single(d => d.Id == other.Id).IsMine.Should().BeFalse();
        result.Value.Single(d => d.Id == transferred.Id).IsMine.Should().BeFalse();
    }

    [Fact]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_SetsIsMineForCallerOnCacheHit()
    {
        Guid callerId = Guid.NewGuid();
        Sense own = TestWords.Shared(callerId, visibleToChildren: true);
        Sense transferred = TestWords.Transferred(visibleToChildren: true);
        // The cached list is shared by all callers of one age group, so it never stores IsMine = true.
        List<PersonalVocabularyWordDto> cachedDtos = [PersonalVocabularyWordMapper.ToDto(own), PersonalVocabularyWordMapper.ToDto(transferred)];
        _cache
            .Setup(c => c.GetAsync("content:vocabulary-shared:False", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedDtos)));

        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult, callerId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Single(d => d.Id == own.Id).IsMine.Should().BeTrue();
        result.Value.Single(d => d.Id == transferred.Id).IsMine.Should().BeFalse();
        _repository.Verify(r => r.GetSharedAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
