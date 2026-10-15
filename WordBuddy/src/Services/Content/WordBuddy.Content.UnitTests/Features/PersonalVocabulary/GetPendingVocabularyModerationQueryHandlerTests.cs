using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class GetPendingVocabularyModerationQueryHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();

    private GetPendingVocabularyModerationQueryHandler CreateHandler() =>
        new(_repository.Object, new GetPendingVocabularyModerationQueryValidator(), NullLogger<GetPendingVocabularyModerationQueryHandler>.Instance);

    [Fact]
    public async Task GetPendingVocabularyModerationQueryHandler_HandleAsync_ReturnsRequestedPageWithTotal()
    {
        Sense word = TestWords.Pending(Guid.NewGuid(), "apple", "a fruit", null);
        _repository
            .Setup(r => r.GetPendingModerationAsync(2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PagedResult<Sense>([word], 2, 10, 11)));

        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetPendingVocabularyModerationQuery(2, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(d => d.Id == word.Id);
        result.Value.Page.Should().Be(2);
        result.Value.TotalCount.Should().Be(11);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 101)]
    public async Task GetPendingVocabularyModerationQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<PersonalVocabularyWordDto>> result = await CreateHandler().HandleAsync(new GetPendingVocabularyModerationQuery(page, pageSize));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.VerifyNoOtherCalls();
    }
}
