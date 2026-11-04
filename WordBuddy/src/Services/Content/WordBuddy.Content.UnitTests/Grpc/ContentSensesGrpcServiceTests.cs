using System.Security.Claims;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Api.Grpc;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;
using WordBuddy.Shared.Protos.Content.V1;

namespace WordBuddy.Content.UnitTests.Grpc;

public sealed class ContentSensesGrpcServiceTests
{
    private readonly Mock<IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>>> _handler = new();
    private readonly ContentSensesGrpcService _service;

    public ContentSensesGrpcServiceTests()
    {
        _service = new ContentSensesGrpcService(_handler.Object, NullLogger<ContentSensesGrpcService>.Instance);
    }

    private static FakeServerCallContext ContextFor(Guid userId, string ageGroup)
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim("age_group", ageGroup)],
            "test");
        DefaultHttpContext http = new() { User = new ClaimsPrincipal(identity) };
        return new FakeServerCallContext(http);
    }

    [Fact]
    public async Task GetSensesByIds_AllFieldsAndNulls_MapsToMessage()
    {
        Guid full = Guid.NewGuid();
        Guid bare = Guid.NewGuid();
        _handler.Setup(h => h.HandleAsync(It.IsAny<GetSensesByIdsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SenseReviewDto>>(
            [
                new SenseReviewDto(full, "cat", "an animal", "A cat.", "a.mp3", "i.png", "my note"),
                new SenseReviewDto(bare, "dog", "another animal", null, null, null, null),
            ]));
        GetSensesByIdsRequest request = new();
        request.SenseIds.Add(full.ToString());
        request.SenseIds.Add(bare.ToString());

        GetSensesByIdsReply reply = await _service.GetSensesByIds(request, ContextFor(Guid.NewGuid(), "Adult"));

        reply.Senses.Should().HaveCount(2);
        SenseReview first = reply.Senses[0];
        first.SenseId.Should().Be(full.ToString());
        first.Word.Should().Be("cat");
        first.Definition.Should().Be("an animal");
        first.Example.Should().Be("A cat.");
        first.AudioUrl.Should().Be("a.mp3");
        first.ImageUrl.Should().Be("i.png");
        first.PersonalContext.Should().Be("my note");
        SenseReview second = reply.Senses[1];
        second.HasExample.Should().BeFalse();
        second.HasAudioUrl.Should().BeFalse();
        second.HasImageUrl.Should().BeFalse();
        second.HasPersonalContext.Should().BeFalse();
    }

    [Fact]
    public async Task GetSensesByIds_BadGuid_ThrowsInvalidArgumentWithoutCallingHandler()
    {
        GetSensesByIdsRequest request = new();
        request.SenseIds.Add("not-a-guid");

        Func<Task> act = () => _service.GetSensesByIds(request, ContextFor(Guid.NewGuid(), "Adult"));

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
        _handler.Verify(h => h.HandleAsync(It.IsAny<GetSensesByIdsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Adult", AgeGroup.Adult)]
    [InlineData("Child", AgeGroup.Child)]
    public async Task GetSensesByIds_ValidRequest_PassesJwtUserIdAndAgeGroupToQuery(string claim, AgeGroup expected)
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        GetSensesByIdsQuery? captured = null;
        _handler.Setup(h => h.HandleAsync(It.IsAny<GetSensesByIdsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetSensesByIdsQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(Result.Success<IReadOnlyList<SenseReviewDto>>([]));
        GetSensesByIdsRequest request = new();
        request.SenseIds.Add(senseId.ToString());

        await _service.GetSensesByIds(request, ContextFor(userId, claim));

        captured.Should().NotBeNull();
        captured!.RequestingUserId.Should().Be(userId);
        captured.RequestingAgeGroup.Should().Be(expected);
        captured.Ids.Should().Equal(senseId);
    }

    [Fact]
    public async Task GetSensesByIds_HandlerValidationFailure_ThrowsInvalidArgument()
    {
        _handler.Setup(h => h.HandleAsync(It.IsAny<GetSensesByIdsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<SenseReviewDto>>(Error.Validation("Sense.Ids", "1 to 100")));

        Func<Task> act = () => _service.GetSensesByIds(new GetSensesByIdsRequest(), ContextFor(Guid.NewGuid(), "Adult"));

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
