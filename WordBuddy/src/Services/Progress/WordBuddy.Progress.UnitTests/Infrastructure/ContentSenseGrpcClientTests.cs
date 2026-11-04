using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Shared.Kernel;
using WordBuddy.Shared.Protos.Content.V1;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class ContentSenseGrpcClientTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ContentSenses.ContentSensesClient> _grpc = new();
    private readonly Mock<IDistributedCache> _cache = new();

    public ContentSenseGrpcClientTests()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
    }

    private ContentSenseGrpcClient CreateClient() => new(
        _grpc.Object,
        new SessionSenseCache(_cache.Object, new FixedTimeProvider(Now)),
        NullLogger<ContentSenseGrpcClient>.Instance);

    private static AsyncUnaryCall<GetSensesByIdsReply> Reply(GetSensesByIdsReply reply) => new(
        Task.FromResult(reply),
        Task.FromResult(new Metadata()),
        () => Status.DefaultSuccess,
        () => [],
        () => { });

    private static AsyncUnaryCall<GetSensesByIdsReply> Failed(StatusCode code) => new(
        Task.FromException<GetSensesByIdsReply>(new RpcException(new Status(code, "x"))),
        Task.FromResult(new Metadata()),
        () => new Status(code, "x"),
        () => [],
        () => { });

    private void SetupCall(Func<GetSensesByIdsRequest, AsyncUnaryCall<GetSensesByIdsReply>> reply) =>
        _grpc.Setup(c => c.GetSensesByIdsAsync(It.IsAny<GetSensesByIdsRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns((GetSensesByIdsRequest request, Metadata? _, DateTime? _, CancellationToken _) => reply(request));

    [Fact]
    public async Task ContentSenseGrpcClient_GetSessionSensesAsync_Reply_MapsAllFieldsAndNulls()
    {
        Guid full = Guid.NewGuid();
        Guid bare = Guid.NewGuid();
        GetSensesByIdsReply reply = new();
        reply.Senses.Add(new SenseReview
        {
            SenseId = full.ToString(),
            Word = "cat",
            Definition = "an animal",
            ImageUrl = "i.png",
            AudioUrl = "a.mp3",
            PersonalContext = "note",
        });
        reply.Senses.Add(new SenseReview { SenseId = bare.ToString(), Word = "dog", Definition = "another" });
        SetupCall(_ => Reply(reply));

        Result<IReadOnlyList<ContentSenseDto>> result = await CreateClient().GetSessionSensesAsync(
            Guid.NewGuid(), [full, bare], Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(
            new ContentSenseDto(full, "cat", "an animal", "i.png", "a.mp3", "note"),
            new ContentSenseDto(bare, "dog", "another", null, null, null));
    }

    [Fact]
    public async Task ContentSenseGrpcClient_GetSessionSensesAsync_150Ids_MakesTwoCallsOf100And50()
    {
        List<int> sizes = [];
        SetupCall(request =>
        {
            sizes.Add(request.SenseIds.Count);
            return Reply(new GetSensesByIdsReply());
        });
        Guid[] ids = Enumerable.Range(0, 150).Select(_ => Guid.NewGuid()).ToArray();

        Result<IReadOnlyList<ContentSenseDto>> result = await CreateClient().GetSessionSensesAsync(Guid.NewGuid(), ids, Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        sizes.Should().Equal(100, 50);
    }

    [Fact]
    public async Task ContentSenseGrpcClient_GetSessionSensesAsync_CacheHit_MakesNoCall()
    {
        Guid id = Guid.NewGuid();
        byte[] cached = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
            new List<ContentSenseDto> { new(id, "cat", "an animal", null, null, null) },
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        Result<IReadOnlyList<ContentSenseDto>> result = await CreateClient().GetSessionSensesAsync(Guid.NewGuid(), [id], Now.AddHours(1));

        result.Value.Should().ContainSingle().Which.Word.Should().Be("cat");
        _grpc.Verify(
            c => c.GetSensesByIdsAsync(It.IsAny<GetSensesByIdsRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.PermissionDenied)]
    public async Task ContentSenseGrpcClient_GetSessionSensesAsync_RpcException_ReturnsContentUnavailable(StatusCode code)
    {
        SetupCall(_ => Failed(code));

        Result<IReadOnlyList<ContentSenseDto>> result = await CreateClient().GetSessionSensesAsync(Guid.NewGuid(), [Guid.NewGuid()], Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Content.Unavailable");
    }

    [Fact]
    public async Task ContentSenseGrpcClient_GetSessionSensesAsync_CircuitOpen_ReturnsContentUnavailable()
    {
        _grpc.Setup(c => c.GetSensesByIdsAsync(It.IsAny<GetSensesByIdsRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new Polly.CircuitBreaker.BrokenCircuitException("open"));

        Result<IReadOnlyList<ContentSenseDto>> result = await CreateClient().GetSessionSensesAsync(Guid.NewGuid(), [Guid.NewGuid()], Now.AddHours(1));

        result.Error.Code.Should().Be("Content.Unavailable");
    }

    [Fact]
    public async Task ContentSenseGrpcClient_GetSessionSensesAsync_CallerCancelled_Propagates()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();
        _grpc.Setup(c => c.GetSensesByIdsAsync(It.IsAny<GetSensesByIdsRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Cancelled, "cancelled")));

        Func<Task> act = () => CreateClient().GetSessionSensesAsync(Guid.NewGuid(), [Guid.NewGuid()], Now.AddHours(1), cts.Token);

        await act.Should().ThrowAsync<RpcException>();
    }
}
