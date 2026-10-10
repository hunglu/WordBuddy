using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-27 group word assignment on real SQL Server: one call gives every active member the
/// words once; a repeat adds nothing; a non-owner gets 403.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class GroupVocabularyEndpointsTests
{
    private readonly ContentApiFactory _factory;

    public GroupVocabularyEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin: false));
        return client;
    }

    private async Task<Guid> SeedSharedSenseAsync()
    {
        Guid lexemeId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        string word = $"group{Guid.NewGuid():N}"[..12];
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        db.Lexemes.Add(Lexeme.Create(lexemeId, word).Value);
        db.Senses.Add(Sense.CreateSystem(senseId, lexemeId, word, "A test meaning.", "A test example."));
        await db.SaveChangesAsync();
        return senseId;
    }

    private async Task SeedMemberAsync(Guid groupId, Guid ownerId, Guid learnerId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        db.LearnerGroupMemberProjections.Add(LearnerGroupMemberProjection.Create(groupId, ownerId, learnerId, isActive: true, DateTime.UtcNow));
        await db.SaveChangesAsync();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, learnerId, ownerId);
    }

    [Fact]
    public async Task GroupWords_Assign_AddsLinksForAllActiveMembersOnceAndNonOwnerIsForbidden()
    {
        Guid owner = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid adult = Guid.NewGuid();
        Guid child = Guid.NewGuid();
        await SeedMemberAsync(groupId, owner, adult);
        await SeedMemberAsync(groupId, owner, child);
        Guid senseId = await SeedSharedSenseAsync();

        HttpResponseMessage first = await Client(owner, "Adult").PostAsJsonAsync($"/api/vocabulary/groups/{groupId}/words", new { senseIds = new[] { senseId } });
        HttpResponseMessage second = await Client(owner, "Adult").PostAsJsonAsync($"/api/vocabulary/groups/{groupId}/words", new { senseIds = new[] { senseId } });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<GroupWordAssignmentResultDto>())!.Added.Should().Be(2);
        (await second.Content.ReadFromJsonAsync<GroupWordAssignmentResultDto>())!.Should().Be(new GroupWordAssignmentResultDto(0, 2, 0));

        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        List<LearnerWord> links = await db.LearnerWords.Where(l => l.SenseId == senseId).ToListAsync();
        links.Select(l => l.UserId).Should().BeEquivalentTo([adult, child]);
        links.Should().OnlyContain(l => l.AddedBy == LearnerWordAddedBy.Supporter && l.AddedByUserId == owner);

        HttpResponseMessage other = await Client(Guid.NewGuid(), "Adult").PostAsJsonAsync($"/api/vocabulary/groups/{groupId}/words", new { senseIds = new[] { senseId } });
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client(Guid.NewGuid(), "Adult").GetAsync($"/api/vocabulary/groups/{groupId}/words")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        List<GroupWordAssignmentDto> history = (await Client(owner, "Adult").GetFromJsonAsync<List<GroupWordAssignmentDto>>($"/api/vocabulary/groups/{groupId}/words"))!;
        history.Should().NotBeEmpty().And.OnlyContain(h => h.SenseId == senseId);
    }
}
