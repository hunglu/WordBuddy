using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemeDetail;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.LexemesAdmin;

public class GetAdminLexemeDetailQueryHandlerTests
{
    private readonly Mock<ILexemeAdminRepository> _repository = new();

    private GetAdminLexemeDetailQueryHandler Handler() =>
        new(_repository.Object, new GetAdminLexemeDetailQueryValidator(), NullLogger<GetAdminLexemeDetailQueryHandler>.Instance);

    [Fact]
    public async Task GetAdminLexemeDetailQueryHandler_HandleAsync_UnknownLexeme_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetDetailAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LexemeDetailData>(Error.NotFound("Lexeme.NotFound", "missing")));

        Result<AdminLexemeDetailDto> result = await Handler().HandleAsync(new GetAdminLexemeDetailQuery(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAdminLexemeDetailQueryHandler_HandleAsync_PrivateSensesAreCountedNotListed()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple", PartOfSpeech.Noun).Value;
        Sense catalog = Sense.CreateSystem(Guid.NewGuid(), lexeme.Id, "Apple", "a round fruit", "I ate an apple.");
        _repository
            .Setup(r => r.GetDetailAsync(lexeme.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LexemeDetailData(lexeme, [catalog], PrivateLearnerSenseCount: 3)));

        Result<AdminLexemeDetailDto> result = await Handler().HandleAsync(new GetAdminLexemeDetailQuery(lexeme.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Senses.Should().ContainSingle().Which.Word.Should().Be("Apple");
        result.Value.CatalogSenseCount.Should().Be(1);
        result.Value.PrivateLearnerSenseCount.Should().Be(3);
        result.Value.Lemma.Should().Be("Apple");
    }

    [Fact]
    public async Task GetAdminLexemeDetailQueryHandler_HandleAsync_NoCatalogSense_FallsBackToLowerCaseLemma()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "SecretSpelling", PartOfSpeech.Noun).Value;
        _repository
            .Setup(r => r.GetDetailAsync(lexeme.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LexemeDetailData(lexeme, [], PrivateLearnerSenseCount: 1)));

        Result<AdminLexemeDetailDto> result = await Handler().HandleAsync(new GetAdminLexemeDetailQuery(lexeme.Id));

        result.Value.Lemma.Should().Be("secretspelling");
        result.Value.Senses.Should().BeEmpty();
    }
}
