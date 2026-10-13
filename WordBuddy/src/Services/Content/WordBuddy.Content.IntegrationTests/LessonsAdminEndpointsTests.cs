using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-47 admin lesson authoring through the real API and SQL Server: auth on every write
/// endpoint, the create, attach, reorder, publish flow, the child filter, and delete keeping senses.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class LessonsAdminEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContentApiFactory _factory;

    public LessonsAdminEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClientAsync(string ageGroup, bool isAdmin = false, Guid? userId = null)
    {
        Guid id = userId ?? Guid.NewGuid();
        if (ageGroup == "Child")
        {
            await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, id);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(id, ageGroup, isAdmin));
        return client;
    }

    private static object NewLessonBody(string title, string target = "Child") => new
    {
        title,
        description = "Integration test lesson.",
        type = "Vocabulary",
        level = "Beginner",
        targetAgeGroup = target,
    };

    private static async Task<Guid> CreateLessonAsync(HttpClient admin, string title, string target = "Child")
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/lessons/admin", NewLessonBody(title, target));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<Guid> SystemSenseIdAsync(HttpClient admin, string word)
    {
        List<AdminSenseSearchResultDto>? found =
            await admin.GetFromJsonAsync<List<AdminSenseSearchResultDto>>($"/api/lessons/admin/senses?search={word}", JsonOptions);
        return found!.First(s => s.Word == word && s.Source == Domain.VocabularySource.System).SenseId;
    }

    /// <summary>An approved shared-pool word; the admin chooses whether children may see it.</summary>
    private async Task<Guid> SharedSenseIdAsync(HttpClient admin, bool visibleToChildren)
    {
        HttpClient owner = await ClientAsync("Adult");
        HttpResponseMessage added = await owner.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest($"shared-{Guid.NewGuid():N}", "a shared definition", null));
        Guid id = await added.Content.ReadFromJsonAsync<Guid>();
        (await owner.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(true, visibleToChildren)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return id;
    }

    private static async Task<AdminLessonDetailDto> AdminDetailAsync(HttpClient admin, Guid lessonId) =>
        (await admin.GetFromJsonAsync<AdminLessonDetailDto>($"/api/lessons/admin/{lessonId}", JsonOptions))!;

    // ---- auth

    public static TheoryData<string, string> WriteEndpoints() => new()
    {
        { "POST", "/api/lessons/admin" },
        { "PUT", "/api/lessons/admin/{id}" },
        { "DELETE", "/api/lessons/admin/{id}" },
        { "POST", "/api/lessons/admin/{id}/publish" },
        { "POST", "/api/lessons/admin/{id}/unpublish" },
        { "POST", "/api/lessons/admin/{id}/senses" },
        { "DELETE", "/api/lessons/admin/{id}/senses/{id}" },
        { "PUT", "/api/lessons/admin/{id}/senses/order" },
        { "GET", "/api/lessons/admin" },
        { "GET", "/api/lessons/admin/{id}" },
        { "GET", "/api/lessons/admin/senses" },
    };

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task LessonsAdminController_NonAdmin_Returns403(string method, string urlTemplate)
    {
        string url = urlTemplate.Replace("{id}", Guid.NewGuid().ToString());
        foreach (string ageGroup in new[] { "Adult", "Child" })
        {
            HttpClient client = await ClientAsync(ageGroup);

            HttpResponseMessage response = await client.SendAsync(
                new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { }) });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{ageGroup} must not reach {method} {urlTemplate}");
        }
    }

    [Fact]
    public async Task LessonsAdminController_Anonymous_Returns401()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync("/api/lessons/admin", NewLessonBody("x"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LessonsController_Post_IsGone()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/lessons", NewLessonBody("old route"));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    // ---- authoring flow

    [Fact]
    public async Task Admin_CreatesDraft_NotVisibleToLearnersUntilPublished()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        HttpClient adult = await ClientAsync("Adult");
        string title = $"Draft {Guid.NewGuid():N}";

        Guid lessonId = await CreateLessonAsync(admin, title);

        AdminLessonDetailDto detail = await AdminDetailAsync(admin, lessonId);
        detail.IsPublished.Should().BeFalse();
        (await adult.GetAsync($"/api/lessons/{lessonId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        List<LessonDto> adultList = (await adult.GetFromJsonAsync<List<LessonDto>>("/api/lessons", JsonOptions))!;
        adultList.Should().NotContain(l => l.Id == lessonId);
        List<AdminLessonDto> adminList = (await admin.GetFromJsonAsync<List<AdminLessonDto>>("/api/lessons/admin?isPublished=false", JsonOptions))!;
        adminList.Should().Contain(l => l.Id == lessonId && !l.IsPublished);

        (await admin.PostAsync($"/api/lessons/admin/{lessonId}/publish", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await adult.GetAsync($"/api/lessons/{lessonId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/lessons/admin/{lessonId}/unpublish", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await adult.GetAsync($"/api/lessons/{lessonId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_Update_ChangesDetails()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid lessonId = await CreateLessonAsync(admin, "Before");

        HttpResponseMessage response = await admin.PutAsJsonAsync(
            $"/api/lessons/admin/{lessonId}", new { title = "After", description = "Changed.", level = "Advanced", targetAgeGroup = "Adult" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        AdminLessonDetailDto detail = await AdminDetailAsync(admin, lessonId);
        detail.Title.Should().Be("After");
        detail.Level.Should().Be(Domain.Level.Advanced);
        detail.TargetAgeGroup.Should().Be(Domain.AgeGroup.Adult);
    }

    [Fact]
    public async Task Admin_UnknownLesson_Returns404()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);

        (await admin.PostAsync($"/api/lessons/admin/{Guid.NewGuid()}/publish", content: null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.DeleteAsync($"/api/lessons/admin/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_CreateWithEmptyTitle_Returns400()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);

        HttpResponseMessage response = await admin.PostAsJsonAsync("/api/lessons/admin", NewLessonBody(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_AttachReorderPublish_LearnerReadsWordsInOrder()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid dog = await SystemSenseIdAsync(admin, "Dog");
        Guid cat = await SystemSenseIdAsync(admin, "Cat");
        Guid bird = await SystemSenseIdAsync(admin, "Bird");
        Guid lessonId = await CreateLessonAsync(admin, $"Flow {Guid.NewGuid():N}");

        foreach (Guid senseId in new[] { dog, cat, bird })
        {
            (await admin.PostAsJsonAsync($"/api/lessons/admin/{lessonId}/senses", new { senseId })).StatusCode
                .Should().Be(HttpStatusCode.NoContent);
        }

        (await admin.PutAsJsonAsync($"/api/lessons/admin/{lessonId}/senses/order", new { senseIds = new[] { bird, dog, cat } }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsync($"/api/lessons/admin/{lessonId}/publish", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpClient adult = await ClientAsync("Adult");
        LessonDetailDto lesson = (await adult.GetFromJsonAsync<LessonDetailDto>($"/api/lessons/{lessonId}", JsonOptions))!;
        lesson.VocabularyItems.Select(v => v.Word).Should().Equal("Bird", "Dog", "Cat");

        // Detach renumbers; the rest keep their order.
        (await admin.DeleteAsync($"/api/lessons/admin/{lessonId}/senses/{dog}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        AdminLessonDetailDto detail = await AdminDetailAsync(admin, lessonId);
        detail.Senses.Select(s => (s.Word, s.SortOrder)).Should().Equal(("Bird", 0), ("Cat", 1));
    }

    [Fact]
    public async Task Admin_Attach_RejectsDuplicatePrivateAndNonVocabularyLessons()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid dog = await SystemSenseIdAsync(admin, "Dog");
        Guid lessonId = await CreateLessonAsync(admin, $"Rules {Guid.NewGuid():N}");
        (await admin.PostAsJsonAsync($"/api/lessons/admin/{lessonId}/senses", new { senseId = dog })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.PostAsJsonAsync($"/api/lessons/admin/{lessonId}/senses", new { senseId = dog })).StatusCode.Should().Be(HttpStatusCode.Conflict);

        HttpClient owner = await ClientAsync("Adult");
        Guid privateSense = await (await owner.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest($"private-{Guid.NewGuid():N}", "mine", null))).Content.ReadFromJsonAsync<Guid>();
        (await admin.PostAsJsonAsync($"/api/lessons/admin/{lessonId}/senses", new { senseId = privateSense })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await admin.PostAsJsonAsync($"/api/lessons/admin/{lessonId}/senses", new { senseId = Guid.NewGuid() })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await admin.PutAsJsonAsync($"/api/lessons/admin/{lessonId}/senses/order", new { senseIds = new[] { Guid.NewGuid() } }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>The race case: the same link is inserted by another writer after the lesson was loaded.
    /// The repository must turn the PK violation into a 409-style conflict, not throw.</summary>
    [Fact]
    public async Task LessonRepository_SaveChanges_DuplicateLessonSenseRow_ReturnsSenseAlreadyAttached()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid dog = await SystemSenseIdAsync(admin, "Dog");
        Guid lessonId = await CreateLessonAsync(admin, $"Race {Guid.NewGuid():N}");

        using IServiceScope scope = _factory.Services.CreateScope();
        ILessonRepository repository = scope.ServiceProvider.GetRequiredService<ILessonRepository>();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        Lesson lesson = (await repository.GetByIdForUpdateAsync(lessonId)).Value;
        Sense sense = await db.Senses.AsTracking().SingleAsync(s => s.Id == dog);
        lesson.AttachSense(sense).IsSuccess.Should().BeTrue();

        using (IServiceScope other = _factory.Services.CreateScope())
        {
            ContentDbContext otherDb = other.ServiceProvider.GetRequiredService<ContentDbContext>();
            await otherDb.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO LessonSenses (LessonId, SenseId, SortOrder) VALUES ({lessonId}, {dog}, 0)");
        }

        Result result = await repository.SaveChangesAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lesson.SenseAlreadyAttached");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ---- child filter

    [Fact]
    public async Task Child_DoesNotSeeHiddenSenseDraftsOrAdultLessons_AdultSeesAll()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid dog = await SystemSenseIdAsync(admin, "Dog");
        Guid hidden = await SharedSenseIdAsync(admin, visibleToChildren: false);
        Guid childLesson = await CreateLessonAsync(admin, $"For child {Guid.NewGuid():N}", "Child");
        Guid adultLesson = await CreateLessonAsync(admin, $"For adult {Guid.NewGuid():N}", "Adult");
        Guid draftLesson = await CreateLessonAsync(admin, $"Draft {Guid.NewGuid():N}", "Child");
        foreach (Guid senseId in new[] { dog, hidden })
        {
            (await admin.PostAsJsonAsync($"/api/lessons/admin/{childLesson}/senses", new { senseId })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await admin.PostAsync($"/api/lessons/admin/{childLesson}/publish", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsync($"/api/lessons/admin/{adultLesson}/publish", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        AdminLessonDetailDto adminView = await AdminDetailAsync(admin, childLesson);
        adminView.Senses.Single(s => s.SenseId == hidden).VisibleToChildren.Should().BeFalse();
        adminView.Senses.Single(s => s.SenseId == dog).VisibleToChildren.Should().BeTrue();

        HttpClient child = await ClientAsync("Child");
        LessonDetailDto childView = (await child.GetFromJsonAsync<LessonDetailDto>($"/api/lessons/{childLesson}", JsonOptions))!;
        childView.VocabularyItems.Select(v => v.Id).Should().Equal(dog);
        (await child.GetAsync($"/api/lessons/{adultLesson}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await child.GetAsync($"/api/lessons/{draftLesson}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        List<LessonDto> childList = (await child.GetFromJsonAsync<List<LessonDto>>("/api/lessons", JsonOptions))!;
        childList.Should().Contain(l => l.Id == childLesson);
        childList.Should().NotContain(l => l.Id == adultLesson || l.Id == draftLesson);
        childList.Should().OnlyContain(l => l.TargetAgeGroup == Domain.AgeGroup.Child);

        HttpClient adult = await ClientAsync("Adult");
        LessonDetailDto adultView = (await adult.GetFromJsonAsync<LessonDetailDto>($"/api/lessons/{childLesson}", JsonOptions))!;
        adultView.VocabularyItems.Select(v => v.Id).Should().Equal(dog, hidden);
        List<LessonDto> adultList = (await adult.GetFromJsonAsync<List<LessonDto>>("/api/lessons", JsonOptions))!;
        adultList.Should().Contain(l => l.Id == adultLesson);
    }

    // ---- delete

    [Fact]
    public async Task Admin_DeleteLesson_RemovesLinksButKeepsSenses()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid dog = await SystemSenseIdAsync(admin, "Dog");
        Guid shared = await SharedSenseIdAsync(admin, visibleToChildren: true);
        Guid lessonId = await CreateLessonAsync(admin, $"To delete {Guid.NewGuid():N}");
        foreach (Guid senseId in new[] { dog, shared })
        {
            (await admin.PostAsJsonAsync($"/api/lessons/admin/{lessonId}/senses", new { senseId })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await admin.DeleteAsync($"/api/lessons/admin/{lessonId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.GetAsync($"/api/lessons/admin/{lessonId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        (await db.LessonSenses.CountAsync(l => l.LessonId == lessonId)).Should().Be(0);
        (await db.Lessons.CountAsync(l => l.Id == lessonId)).Should().Be(0);
        (await db.Senses.CountAsync(s => s.Id == dog || s.Id == shared)).Should().Be(2);
    }
}
