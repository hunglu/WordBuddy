using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Protos.Content.V1;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// WB-68: the internal gRPC channel <c>ContentSenses.GetSensesByIds</c> against the real host and a
/// real SQL Server. It must return what REST <c>GET /api/vocabulary/senses</c> returns for the same
/// caller and data, with the same child and adult rules.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class ContentSensesGrpcTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContentApiFactory _factory;

    public ContentSensesGrpcTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private GrpcChannel CreateChannel() => GrpcChannel.ForAddress(
        _factory.Server.BaseAddress,
        new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });

    private static Metadata Bearer(Guid userId, string ageGroup, bool isAdmin = false) =>
        [new Metadata.Entry("authorization", $"Bearer {TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin)}")];

    private HttpClient RestClient(Guid userId, string ageGroup, bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    private static GetSensesByIdsRequest RequestFor(params Guid[] ids)
    {
        GetSensesByIdsRequest request = new();
        request.SenseIds.AddRange(ids.Select(id => id.ToString()));
        return request;
    }

    private static async Task<Guid> AddPrivateWordAsync(HttpClient client, string prefix)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest($"{prefix}-{Guid.NewGuid():N}", "a meaning", "An example."));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>Shares and approves a word so other callers can see it.</summary>
    private async Task<Guid> AddSharedWordAsync(bool visibleToChildren)
    {
        HttpClient author = RestClient(Guid.NewGuid(), "Adult");
        HttpClient admin = RestClient(Guid.NewGuid(), "Adult", isAdmin: true);
        Guid id = await AddPrivateWordAsync(author, "shared");
        (await author.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: visibleToChildren)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return id;
    }

    [Fact]
    public async Task GetSensesByIds_AdultOwnWord_ReturnsSameSenseAsRest()
    {
        Guid userId = Guid.NewGuid();
        HttpClient rest = RestClient(userId, "Adult");
        Guid id = await AddPrivateWordAsync(rest, "own");
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);

        GetSensesByIdsReply reply = await client.GetSensesByIdsAsync(RequestFor(id), Bearer(userId, "Adult"));

        List<SenseReviewDto>? restSenses = await (await rest.GetAsync($"/api/vocabulary/senses?ids={id}"))
            .Content.ReadFromJsonAsync<List<SenseReviewDto>>(JsonOptions);
        SenseReview sense = reply.Senses.Should().ContainSingle().Subject;
        SenseReviewDto expected = restSenses.Should().ContainSingle().Subject;
        sense.SenseId.Should().Be(expected.SenseId.ToString());
        sense.Word.Should().Be(expected.Word);
        sense.Definition.Should().Be(expected.Definition);
        sense.Example.Should().Be(expected.Example);
        sense.HasAudioUrl.Should().BeFalse();
        sense.HasImageUrl.Should().BeFalse();
        sense.HasPersonalContext.Should().BeFalse();
    }

    [Fact]
    public async Task GetSensesByIds_ChildWithSupporter_ReturnsOnlyChildVisibleSenses()
    {
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId);
        Guid visible = await AddSharedWordAsync(visibleToChildren: true);
        Guid hidden = await AddSharedWordAsync(visibleToChildren: false);
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);

        GetSensesByIdsReply reply = await client.GetSensesByIdsAsync(RequestFor(visible, hidden), Bearer(childId, "Child"));

        reply.Senses.Select(s => s.SenseId).Should().Equal(visible.ToString());
    }

    [Fact]
    public async Task GetSensesByIds_AdultSeesChildHiddenSharedSense()
    {
        Guid hidden = await AddSharedWordAsync(visibleToChildren: false);
        Guid adultId = Guid.NewGuid();
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);

        GetSensesByIdsReply reply = await client.GetSensesByIdsAsync(RequestFor(hidden), Bearer(adultId, "Adult"));

        reply.Senses.Select(s => s.SenseId).Should().Equal(hidden.ToString());
    }

    [Fact]
    public async Task GetSensesByIds_ChildWithoutSupporter_ReturnsPermissionDenied()
    {
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);

        Func<Task> act = async () => await client.GetSensesByIdsAsync(RequestFor(Guid.NewGuid()), Bearer(Guid.NewGuid(), "Child"));

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetSensesByIds_NoToken_ReturnsUnauthenticated()
    {
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);

        Func<Task> act = async () => await client.GetSensesByIdsAsync(RequestFor(Guid.NewGuid()));

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetSensesByIds_IdCountOutOfRange_ReturnsInvalidArgument(int count)
    {
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);
        Guid[] ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

        Func<Task> act = async () => await client.GetSensesByIdsAsync(RequestFor(ids), Bearer(Guid.NewGuid(), "Adult"));

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetSensesByIds_NotAGuid_ReturnsInvalidArgument()
    {
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);
        GetSensesByIdsRequest request = new();
        request.SenseIds.Add("not-a-guid");

        Func<Task> act = async () => await client.GetSensesByIdsAsync(request, Bearer(Guid.NewGuid(), "Adult"));

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetSensesByIds_ForeignPrivateAndUnknownIds_AreOmittedWithoutError()
    {
        HttpClient stranger = RestClient(Guid.NewGuid(), "Adult");
        Guid foreignPrivate = await AddPrivateWordAsync(stranger, "secret");
        using GrpcChannel channel = CreateChannel();
        ContentSenses.ContentSensesClient client = new(channel);

        GetSensesByIdsReply reply = await client.GetSensesByIdsAsync(
            RequestFor(foreignPrivate, Guid.NewGuid()), Bearer(Guid.NewGuid(), "Adult"));

        reply.Senses.Should().BeEmpty();
    }

    [Fact]
    public async Task GrpcHealth_Check_ReturnsServing()
    {
        using GrpcChannel channel = CreateChannel();
        Health.HealthClient client = new(channel);

        // The health publisher updates the status shortly after startup; poll instead of sleeping.
        HealthCheckResponse.Types.ServingStatus status = HealthCheckResponse.Types.ServingStatus.Unknown;
        for (int attempt = 0; attempt < 40 && status != HealthCheckResponse.Types.ServingStatus.Serving; attempt++)
        {
            status = (await client.CheckAsync(new HealthCheckRequest())).Status;
            if (status != HealthCheckResponse.Types.ServingStatus.Serving)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }

        status.Should().Be(HealthCheckResponse.Types.ServingStatus.Serving);
    }

    [Fact]
    public async Task RestEndpoints_HealthAndSenses_StillWork()
    {
        Guid userId = Guid.NewGuid();
        HttpClient rest = RestClient(userId, "Adult");
        Guid id = await AddPrivateWordAsync(rest, "rest");

        HttpResponseMessage health = await _factory.CreateClient().GetAsync("/health");
        HttpResponseMessage senses = await rest.GetAsync($"/api/vocabulary/senses?ids={id}");

        health.StatusCode.Should().Be(HttpStatusCode.OK);
        senses.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
