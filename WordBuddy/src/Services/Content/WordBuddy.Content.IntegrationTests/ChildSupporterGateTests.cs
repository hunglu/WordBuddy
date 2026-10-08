using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-24 <c>ChildHasSupporter</c> on Content learning endpoints, real SQL Server.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class ChildSupporterGateTests
{
    private readonly ContentApiFactory _factory;

    public ChildSupporterGateTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup, bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    [Theory]
    [InlineData("/api/lessons")]
    [InlineData("/api/vocabulary/mine")]
    public async Task ChildHasSupporter_ChildWithoutLink_Returns403SupporterRequired(string url)
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Child").GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("Learner.SupporterRequired");
    }

    [Theory]
    [InlineData("/api/lessons")]
    [InlineData("/api/vocabulary/mine")]
    public async Task ChildHasSupporter_ChildWithLink_Returns200(string url)
    {
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId);

        HttpResponseMessage response = await Client(childId, "Child").GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChildHasSupporter_AdultWithoutLink_Returns200()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Adult").GetAsync("/api/lessons");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
