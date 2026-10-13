using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-47: an admin authors a lesson through Content's admin endpoints, and learners read it in the
/// order the admin set. Needs the backend up (Identity + Content). The admin login comes from the
/// Development seed, like the other suites (override with <c>E2E_ADMIN_EMAIL</c>/<c>E2E_ADMIN_PASSWORD</c>).
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class LessonAuthoringTests
{
    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private const string Password = "ChangeMe123!";

    private readonly ApiRequestContextFixture _fixture;

    public LessonAuthoringTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminCreatesAttachesReordersPublishes_LearnerReadsWordsInOrder()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            string adultToken = await RegisterAsync(identity, "Adult");

            Guid dog = await SystemSenseIdAsync(content, adminToken, "Dog");
            Guid cat = await SystemSenseIdAsync(content, adminToken, "Cat");
            Guid bird = await SystemSenseIdAsync(content, adminToken, "Bird");

            IAPIResponse create = await content.PostAsync("/api/lessons/admin", Options(adminToken, new
            {
                title = $"E2E lesson {Guid.NewGuid():N}",
                description = "Created by the WB-47 API E2E test.",
                type = "Vocabulary",
                level = "Beginner",
                targetAgeGroup = "Adult",
            }));
            create.Status.Should().Be(201, await create.TextAsync());
            Guid lessonId = (await create.JsonAsync())!.Value.GetGuid();

            // A new lesson is a draft: learners cannot read it.
            (await content.GetAsync($"/api/lessons/{lessonId}", Options(adultToken))).Status.Should().Be(404);

            foreach (Guid senseId in new[] { dog, cat, bird })
            {
                IAPIResponse attach = await content.PostAsync($"/api/lessons/admin/{lessonId}/senses", Options(adminToken, new { senseId }));
                attach.Status.Should().Be(204, await attach.TextAsync());
            }

            IAPIResponse reorder = await content.PutAsync(
                $"/api/lessons/admin/{lessonId}/senses/order", Options(adminToken, new { senseIds = new[] { bird, dog, cat } }));
            reorder.Status.Should().Be(204, await reorder.TextAsync());

            IAPIResponse publish = await content.PostAsync($"/api/lessons/admin/{lessonId}/publish", Options(adminToken));
            publish.Status.Should().Be(204, await publish.TextAsync());

            IAPIResponse read = await content.GetAsync($"/api/lessons/{lessonId}", Options(adultToken));
            read.Status.Should().Be(200, await read.TextAsync());
            JsonElement lesson = (await read.JsonAsync())!.Value;
            lesson.GetProperty("vocabularyItems").EnumerateArray()
                .Select(i => i.GetProperty("word").GetString())
                .Should().Equal("Bird", "Dog", "Cat");

            (await content.DeleteAsync($"/api/lessons/admin/{lessonId}", Options(adminToken))).Status.Should().Be(204);
            (await content.GetAsync($"/api/lessons/{lessonId}", Options(adultToken))).Status.Should().Be(404);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminLessonWrites_NonAdminCaller_ReturnsForbidden()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adultToken = await RegisterAsync(identity, "Adult");

            IAPIResponse create = await content.PostAsync("/api/lessons/admin", Options(adultToken, new
            {
                title = "Not allowed",
                description = "Should be rejected.",
                type = "Vocabulary",
                level = "Beginner",
                targetAgeGroup = "Child",
            }));
            create.Status.Should().Be(403);

            (await content.GetAsync("/api/lessons/admin", Options(adultToken))).Status.Should().Be(403);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task<Guid> SystemSenseIdAsync(IAPIRequestContext content, string adminToken, string word)
    {
        IAPIResponse response = await content.GetAsync($"/api/lessons/admin/senses?search={word}", Options(adminToken));
        response.Ok.Should().BeTrue(await response.TextAsync());
        JsonElement match = (await response.JsonAsync())!.Value.EnumerateArray()
            .First(s => s.GetProperty("word").GetString() == word && s.GetProperty("source").GetString() == "System");
        return match.GetProperty("senseId").GetGuid();
    }

    private static async Task<string> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"lesson-e2e-{Guid.NewGuid():N}@example.com",
                password = Password,
                displayName = "E2E Lesson Reader",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", new APIRequestContextOptions
        {
            DataObject = new { email, password },
        });
        response.Ok.Should().BeTrue($"admin login should succeed (Development seed), got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static APIRequestContextOptions Options(string token, object? body = null) => new()
    {
        Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
        DataObject = body,
    };
}
