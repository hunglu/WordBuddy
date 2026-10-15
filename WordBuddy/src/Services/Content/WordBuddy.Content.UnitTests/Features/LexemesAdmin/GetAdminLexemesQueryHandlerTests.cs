using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemes;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.LexemesAdmin;

public class GetAdminLexemesQueryHandlerTests
{
    private readonly Mock<ILexemeAdminRepository> _repository = new();

    private GetAdminLexemesQueryHandler Handler() =>
        new(_repository.Object, new GetAdminLexemesQueryValidator(), NullLogger<GetAdminLexemesQueryHandler>.Instance);

    [Fact]
    public async Task GetAdminLexemesQueryHandler_HandleAsync_PassesNormalizedSearchAndPagingToRepository()
    {
        PagedResult<AdminLexemeListItemDto> page = new([], 3, 10, 0);
        _repository
            .Setup(r => r.SearchAsync("APPLE", PartOfSpeech.Noun, CefrLevel.A1, 3, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(page));

        Result<PagedResult<AdminLexemeListItemDto>> result = await Handler().HandleAsync(
            new GetAdminLexemesQuery("  apple ", PartOfSpeech.Noun, CefrLevel.A1, 3, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(page);
    }

    [Fact]
    public async Task GetAdminLexemesQueryHandler_HandleAsync_EmptySearch_PassesNull()
    {
        _repository
            .Setup(r => r.SearchAsync(null, null, null, 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PagedResult<AdminLexemeListItemDto>([], 1, 25, 0)));

        Result<PagedResult<AdminLexemeListItemDto>> result = await Handler().HandleAsync(new GetAdminLexemesQuery("   ", null, null));

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public async Task GetAdminLexemesQueryHandler_HandleAsync_InvalidPageSize_ReturnsValidation(int pageSize)
    {
        Result<PagedResult<AdminLexemeListItemDto>> result = await Handler().HandleAsync(new GetAdminLexemesQuery(null, null, null, 1, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAdminLexemesQueryHandler_HandleAsync_PageZero_ReturnsValidation()
    {
        Result<PagedResult<AdminLexemeListItemDto>> result = await Handler().HandleAsync(new GetAdminLexemesQuery(null, null, null, 0, 25));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetAdminLexemesQueryHandler_HandleAsync_SearchLongerThan200_ReturnsValidation()
    {
        Result<PagedResult<AdminLexemeListItemDto>> result = await Handler().HandleAsync(
            new GetAdminLexemesQuery(new string('a', Lexeme.LemmaMaxLength + 1), null, null));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAdminLexemesQueryHandler_HandleAsync_RepositoryFailure_IsPassedThrough()
    {
        Error error = Error.Failure("Test.Failure", "boom");
        _repository
            .Setup(r => r.SearchAsync(It.IsAny<string?>(), It.IsAny<PartOfSpeech?>(), It.IsAny<CefrLevel?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PagedResult<AdminLexemeListItemDto>>(error));

        Result<PagedResult<AdminLexemeListItemDto>> result = await Handler().HandleAsync(new GetAdminLexemesQuery(null, null, null));

        result.Error.Should().Be(error);
    }
}
