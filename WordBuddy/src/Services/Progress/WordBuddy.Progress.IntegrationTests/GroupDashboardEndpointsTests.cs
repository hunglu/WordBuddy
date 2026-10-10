using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>WB-27 group dashboard on real SQL Server: the owner gets 200 with the active, still-supported
/// members; another supporter, an unknown group and a child get 403.</summary>
[Collection(ProgressApiCollection.Name)]
public sealed class GroupDashboardEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ProgressApiFactory _factory;

    public GroupDashboardEndpointsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup));
        return client;
    }

    private async Task SeedMemberAsync(Guid groupId, Guid ownerId, Guid learnerId, bool isActive, bool linkActive)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        dbContext.LearnerGroupMemberProjections.Add(LearnerGroupMemberProjection.Create(groupId, ownerId, learnerId, isActive, DateTime.UtcNow));
        dbContext.SupportLinkProjections.Add(SupportLinkProjection.Create(Guid.NewGuid(), learnerId, ownerId, linkActive, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GroupDashboard_Owner_Returns200WithActiveSupportedMembersOnly()
    {
        Guid owner = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid adult = Guid.NewGuid();
        Guid child = Guid.NewGuid();
        await SeedMemberAsync(groupId, owner, adult, isActive: true, linkActive: true);
        await SeedMemberAsync(groupId, owner, child, isActive: true, linkActive: true);
        await SeedMemberAsync(groupId, owner, Guid.NewGuid(), isActive: false, linkActive: true);
        await SeedMemberAsync(groupId, owner, Guid.NewGuid(), isActive: true, linkActive: false);

        HttpResponseMessage response = await Client(owner, "Adult").GetAsync($"/api/progress/dashboard/groups/{groupId}?days=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GroupDashboardDto dashboard = (await response.Content.ReadFromJsonAsync<GroupDashboardDto>(Json))!;
        dashboard.Days.Should().Be(7);
        dashboard.Members.Select(m => m.LearnerId).Should().BeEquivalentTo([adult, child]);
    }

    [Fact]
    public async Task GroupDashboard_OtherSupporterUnknownGroupAndChild_Return403()
    {
        Guid owner = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        await SeedMemberAsync(groupId, owner, Guid.NewGuid(), isActive: true, linkActive: true);

        (await Client(Guid.NewGuid(), "Adult").GetAsync($"/api/progress/dashboard/groups/{groupId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client(owner, "Adult").GetAsync($"/api/progress/dashboard/groups/{Guid.NewGuid()}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client(Guid.NewGuid(), "Child").GetAsync($"/api/progress/dashboard/groups/{groupId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GroupDashboard_InvalidDays_Returns400()
    {
        Guid owner = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        await SeedMemberAsync(groupId, owner, Guid.NewGuid(), isActive: true, linkActive: true);

        HttpResponseMessage response = await Client(owner, "Adult").GetAsync($"/api/progress/dashboard/groups/{groupId}?days=14");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("title").GetString().Should().Be("GetGroupDashboard.Validation");
    }
}
