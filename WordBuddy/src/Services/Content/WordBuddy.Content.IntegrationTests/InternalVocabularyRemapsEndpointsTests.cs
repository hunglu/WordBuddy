using System.Data.SqlTypes;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// <c>/internal/vocabulary-remaps</c>: only a <c>wb_service=progress</c> token gets in (401 without a
/// token, 403 for Child/Adult learners and admins), pending rows come back ordered by old id (SQL
/// Server's uniqueidentifier order), acknowledged rows disappear, and acknowledging twice is still
/// 204. Real SQL Server, never mocked EF.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class InternalVocabularyRemapsEndpointsTests
{
    private const string PendingUrl = "/internal/vocabulary-remaps?limit=500";
    private const string AcknowledgeUrl = "/internal/vocabulary-remaps/acknowledge";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ContentApiFactory _factory;

    public InternalVocabularyRemapsEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string? token)
    {
        HttpClient client = _factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private async Task<List<Guid>> SeedPendingRemapsAsync(int count)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();

        List<Guid> oldIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
        foreach (Guid oldId in oldIds)
        {
            dbContext.VocabularyWordIdRemaps.Add(new VocabularyWordIdRemap(oldId, Guid.NewGuid()));
        }

        await dbContext.SaveChangesAsync();
        return oldIds;
    }

    private static async Task<List<VocabularyWordIdRemapDto>> GetPendingAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync(PendingUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PendingVocabularyWordIdRemapsResponse? body = await response.Content.ReadFromJsonAsync<PendingVocabularyWordIdRemapsResponse>(JsonOptions);
        return body!.Items.ToList();
    }

    [Fact]
    public async Task InternalVocabularyRemaps_Get_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await CreateClient(token: null).GetAsync(PendingUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Child", false)]
    [InlineData("Adult", false)]
    [InlineData("Adult", true)]
    public async Task InternalVocabularyRemaps_Get_WithLearnerOrAdminToken_Returns403(string ageGroup, bool isAdmin)
    {
        HttpClient client = CreateClient(TestJwtTokenFactory.CreateToken(Guid.NewGuid(), ageGroup, isAdmin));

        (await client.GetAsync(PendingUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync(AcknowledgeUrl, new AcknowledgeVocabularyWordIdRemapsRequest([Guid.NewGuid()])))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InternalVocabularyRemaps_Get_WithOtherServiceToken_Returns403()
    {
        HttpClient client = CreateClient(TestJwtTokenFactory.CreateServiceToken("quiz"));

        (await client.GetAsync(PendingUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InternalVocabularyRemaps_GetAndAcknowledge_WithServiceToken_ReturnsOrderedPendingAndAcknowledgesIdempotently()
    {
        HttpClient client = CreateClient(TestJwtTokenFactory.CreateServiceToken());
        List<Guid> seeded = await SeedPendingRemapsAsync(5);

        List<VocabularyWordIdRemapDto> pending = await GetPendingAsync(client);
        List<Guid> ours = pending.Select(r => r.OldId).Where(seeded.Contains).ToList();
        ours.Should().Equal(seeded.OrderBy(id => new SqlGuid(id)));
        pending.Select(r => new SqlGuid(r.OldId)).Should().BeInAscendingOrder();

        List<Guid> toAck = seeded.Take(3).ToList();
        (await client.PostAsJsonAsync(AcknowledgeUrl, new AcknowledgeVocabularyWordIdRemapsRequest(toAck)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync(AcknowledgeUrl, new AcknowledgeVocabularyWordIdRemapsRequest(toAck)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        List<Guid> remaining = (await GetPendingAsync(client)).Select(r => r.OldId).ToList();
        remaining.Should().NotContain(toAck).And.Contain(seeded.Skip(3));

        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        int stamped = await dbContext.VocabularyWordIdRemaps.CountAsync(r => toAck.Contains(r.OldId) && r.PublishedAtUtc != null);
        stamped.Should().Be(3);
    }

    [Fact]
    public async Task InternalVocabularyRemaps_Acknowledge_UnknownIds_Returns204()
    {
        HttpClient client = CreateClient(TestJwtTokenFactory.CreateServiceToken());

        (await client.PostAsJsonAsync(AcknowledgeUrl, new AcknowledgeVocabularyWordIdRemapsRequest([Guid.NewGuid()])))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task InternalVocabularyRemaps_Get_LimitOutOfRange_Returns400(int limit)
    {
        HttpClient client = CreateClient(TestJwtTokenFactory.CreateServiceToken());

        (await client.GetAsync($"/internal/vocabulary-remaps?limit={limit}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InternalVocabularyRemaps_Acknowledge_EmptyList_Returns400()
    {
        HttpClient client = CreateClient(TestJwtTokenFactory.CreateServiceToken());

        (await client.PostAsJsonAsync(AcknowledgeUrl, new AcknowledgeVocabularyWordIdRemapsRequest([])))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
