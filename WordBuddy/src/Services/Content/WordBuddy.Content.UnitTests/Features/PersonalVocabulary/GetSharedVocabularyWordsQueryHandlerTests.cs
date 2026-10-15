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

    private GetSharedVocabularyWordsQueryHandler CreateHandler() => new(_repository.Object, _cache.Object, new GetSharedVocabularyWordsQueryValidator(), _logger.Object);

    private static Sense CreateSharedWord(bool visibleToChildren) => TestWords.Shared(Guid.NewGuid(), visibleToChildren);

    [Fact]
    public async Task HandleAsync_AdultCaller_ReceivesAllSharedItems()
    {
        _repository
            .Setup(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([CreateSharedWord(true), CreateSharedWord(false)]));

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        _repository.Verify(r => r.GetSharedAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ChildCaller_OnlyReceivesVisibleToChildrenItems()
    {
        _repository
            .Setup(r => r.GetSharedAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([CreateSharedWord(true)]));

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Child, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.VisibleToChildren);
        result.Value.TotalCount.Should().Be(1);
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

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult, callerId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single(d => d.Id == own.Id).IsMine.Should().BeTrue();
        result.Value.Items.Single(d => d.Id == other.Id).IsMine.Should().BeFalse();
        result.Value.Items.Single(d => d.Id == transferred.Id).IsMine.Should().BeFalse();
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

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetSharedVocabularyWordsQuery(AgeGroup.Adult, callerId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single(d => d.Id == own.Id).IsMine.Should().BeTrue();
        result.Value.Items.Single(d => d.Id == transferred.Id).IsMine.Should().BeFalse();
        _repository.Verify(r => r.GetSharedAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void CacheHit(string key, IEnumerable<PersonalVocabularyWordDto> dtos) =>
        _cache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dtos.ToList())));

    [Fact]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_CacheHit_SlicesPageAndKeepsTotal()
    {
        List<PersonalVocabularyWordDto> pool = Enumerable.Range(0, 5)
            .Select(i => PersonalVocabularyWordMapper.ToDto(TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, word: $"word{i}")))
            .ToList();
        CacheHit("content:vocabulary-shared:False", pool);

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(
            new GetSharedVocabularyWordsQuery(AgeGroup.Adult, Guid.NewGuid(), null, 2, 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(d => d.Word).Should().Equal("word2", "word3");
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_PastTheEnd_ReturnsEmptyItemsAndRealTotal()
    {
        List<PersonalVocabularyWordDto> pool = [PersonalVocabularyWordMapper.ToDto(TestWords.Shared(Guid.NewGuid(), visibleToChildren: true))];
        CacheHit("content:vocabulary-shared:False", pool);

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(
            new GetSharedVocabularyWordsQuery(AgeGroup.Adult, Guid.NewGuid(), null, 9, 25));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_Search_FiltersCaseInsensitiveBeforePaging()
    {
        List<PersonalVocabularyWordDto> pool =
        [
            PersonalVocabularyWordMapper.ToDto(TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, word: "Apple")),
            PersonalVocabularyWordMapper.ToDto(TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, word: "banana")),
            PersonalVocabularyWordMapper.ToDto(TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, word: "pineapple")),
        ];
        CacheHit("content:vocabulary-shared:False", pool);

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(
            new GetSharedVocabularyWordsQuery(AgeGroup.Adult, Guid.NewGuid(), "APPLE", 1, 25));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(d => d.Word).Should().BeEquivalentTo("Apple", "pineapple");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_Child_UsesChildCacheSoTotalExcludesNonChildWords()
    {
        List<PersonalVocabularyWordDto> childPool = [PersonalVocabularyWordMapper.ToDto(TestWords.Shared(Guid.NewGuid(), visibleToChildren: true))];
        CacheHit("content:vocabulary-shared:True", childPool);

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(
            new GetSharedVocabularyWordsQuery(AgeGroup.Child, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        _repository.Verify(r => r.GetSharedAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 101)]
    public async Task GetSharedVocabularyWordsQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(
            new GetSharedVocabularyWordsQuery(AgeGroup.Adult, Guid.NewGuid(), null, page, pageSize));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
