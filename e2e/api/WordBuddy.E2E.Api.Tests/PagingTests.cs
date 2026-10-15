using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-55: list endpoints accept <c>page</c>/<c>pageSize</c> and return
/// <c>{ items, page, pageSize, totalCount }</c>. Covers the shared contract (defaults, validation, page
/// past the end, page 2), the new group routes, and the child vs adult shared pool. Needs Identity, Content
/// and Progress up. The admin login comes from the Development seed.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class PagingTests
{
    private const string Password = "ChangeMe123!";

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public PagingTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MyWords_TwoWordsPageSizeOne_PagesAreDistinctAndTotalCountIsTwo()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        Guid first = await AddWordAsync(c.Content, token);
        Guid second = await AddWordAsync(c.Content, token);

        JsonElement page1 = await GetPageAsync(c.Content, "/api/vocabulary/mine?page=1&pageSize=1", token);
        JsonElement page2 = await GetPageAsync(c.Content, "/api/vocabulary/mine?page=2&pageSize=1", token);

        page1.GetProperty("totalCount").GetInt32().Should().Be(2);
        page1.GetProperty("pageSize").GetInt32().Should().Be(1);
        page2.GetProperty("page").GetInt32().Should().Be(2);
        page1.GetProperty("items").GetArrayLength().Should().Be(1);
        page2.GetProperty("items").GetArrayLength().Should().Be(1);
        List<Guid> ids =
        [
            page1.GetProperty("items")[0].GetProperty("id").GetGuid(),
            page2.GetProperty("items")[0].GetProperty("id").GetGuid(),
        ];
        ids.Should().BeEquivalentTo(new[] { first, second });
    }

    [Fact]
    public async Task MyWords_PagePastTheEnd_ReturnsEmptyItemsWithRealTotal()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        await AddWordAsync(c.Content, token);

        JsonElement page = await GetPageAsync(c.Content, "/api/vocabulary/mine?page=9&pageSize=10", token);

        page.GetProperty("items").GetArrayLength().Should().Be(0);
        page.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task MyWords_InvalidPagingParameters_Return400(string query)
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await c.Content.GetAsync($"/api/vocabulary/mine?{query}", new() { Headers = Auth(token) });

        response.Status.Should().Be(400, await response.TextAsync());
    }

    [Fact]
    public async Task MyWords_NoPagingParameters_UsesDefaults()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        JsonElement page = await GetPageAsync(c.Content, "/api/vocabulary/mine", token);

        page.GetProperty("page").GetInt32().Should().Be(1);
        page.GetProperty("pageSize").GetInt32().Should().Be(25);
    }

    [Fact]
    public async Task AdminLessons_AdminGetsPagedShape_AdultGets403()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string admin = await LoginAsync(c.Identity, AdminEmail, AdminPassword);
        string adult = await RegisterAsync(c.Identity, "Adult");

        JsonElement page = await GetPageAsync(c.Content, "/api/lessons/admin?page=1&pageSize=5", admin);
        IAPIResponse forbidden = await c.Content.GetAsync("/api/lessons/admin?page=1&pageSize=5", new() { Headers = Auth(adult) });

        page.GetProperty("pageSize").GetInt32().Should().Be(5);
        page.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(5);
        forbidden.Status.Should().Be(403);
    }

    [Fact]
    public async Task SharedPool_PageSizeOne_ReturnsOneItemAndSearchNarrowsTotal()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string adult = await RegisterAsync(c.Identity, "Adult");

        JsonElement page = await GetPageAsync(c.Content, "/api/vocabulary/shared?page=1&pageSize=1", adult);
        JsonElement none = await GetPageAsync(c.Content, $"/api/vocabulary/shared?search=zz{Guid.NewGuid():N}", adult);

        page.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(1);
        none.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task SharedPool_ChildTotalIsNotLargerThanAdultTotal()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string adult = await RegisterAsync(c.Identity, "Adult");
        string child = await RegisterAsync(c.Identity, "Child");

        JsonElement adultPage = await GetPageAsync(c.Content, "/api/vocabulary/shared?pageSize=100", adult);
        JsonElement childPage = await GetPageAsync(c.Content, "/api/vocabulary/shared?pageSize=100", child);

        childPage.GetProperty("totalCount").GetInt32().Should().BeLessThanOrEqualTo(adultPage.GetProperty("totalCount").GetInt32());
        foreach (JsonElement word in childPage.GetProperty("items").EnumerateArray())
        {
            word.GetProperty("visibleToChildren").GetBoolean().Should().BeTrue();
        }
    }

    [Fact]
    public async Task Groups_JoinedAndApprovals_ReturnPagedShape_OldRouteIsGone()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string adult = await RegisterAsync(c.Identity, "Adult");

        JsonElement joined = await GetPageAsync(c.Identity, "/api/auth/groups/joined?page=1&pageSize=5", adult);
        JsonElement approvals = await GetPageAsync(c.Identity, "/api/auth/groups/approvals?page=1&pageSize=5", adult);
        IAPIResponse oldRoute = await c.Identity.GetAsync("/api/auth/groups", new() { Headers = Auth(adult) });

        joined.GetProperty("totalCount").GetInt32().Should().Be(0);
        approvals.GetProperty("totalCount").GetInt32().Should().Be(0);
        oldRoute.Status.Should().BeOneOf(404, 405);
    }

    [Fact]
    public async Task Groups_ChildOwnedPage_IsEmptyOrForbidden()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string child = await RegisterAsync(c.Identity, "Child");

        IAPIResponse response = await c.Identity.GetAsync("/api/auth/groups/owned?page=1&pageSize=5", new() { Headers = Auth(child) });

        if (response.Status == 200)
        {
            (await response.JsonAsync())!.Value.GetProperty("totalCount").GetInt32().Should().Be(0);
        }
        else
        {
            response.Status.Should().Be(403, "a child owns no groups");
        }
    }

    [Fact]
    public async Task Challenges_AdultListIsPaged()
    {
        await using Ctx c = await Ctx.CreateAsync(_fixture);
        string adult = await RegisterAsync(c.Identity, "Adult");

        JsonElement page = await GetPageAsync(c.Progress, "/api/progress/challenges?page=1&pageSize=2", adult);

        page.GetProperty("pageSize").GetInt32().Should().Be(2);
        page.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(2);
    }

    private static async Task<JsonElement> GetPageAsync(IAPIRequestContext api, string path, string token)
    {
        IAPIResponse response = await api.GetAsync(path, new() { Headers = Auth(token) });
        response.Status.Should().Be(200, $"GET {path}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value.Clone();
        body.EnumerateObject().Select(p => p.Name).Should().Contain(["items", "page", "pageSize", "totalCount"]);
        return body;
    }

    private static async Task<Guid> AddWordAsync(IAPIRequestContext content, string token)
    {
        IAPIResponse response = await content.PostAsync("/api/vocabulary", new()
        {
            Headers = Auth(token),
            DataObject = new { word = $"paging-{Guid.NewGuid():N}", definition = "a word for paging tests", example = (string?)null },
        });
        response.Ok.Should().BeTrue(await response.TextAsync());
        return (await response.JsonAsync())!.Value.GetGuid();
    }

    private static async Task<string> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new()
        {
            DataObject = new
            {
                email = $"paging-e2e-{Guid.NewGuid():N}@example.com",
                password = Password,
                displayName = "E2E Paging Learner",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue(await response.TextAsync());
        string token = (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
        if (ageGroup == "Child")
        {
            await ChildSupport.LinkSupporterAsync(identity, token);
        }

        return token;
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", new() { DataObject = new { email, password } });
        response.Ok.Should().BeTrue(await response.TextAsync());
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };

    private sealed class Ctx : IAsyncDisposable
    {
        public IAPIRequestContext Identity { get; private init; } = null!;

        public IAPIRequestContext Content { get; private init; } = null!;

        public IAPIRequestContext Progress { get; private init; } = null!;

        public static async Task<Ctx> CreateAsync(ApiRequestContextFixture fixture) => new()
        {
            Identity = await fixture.NewContextAsync(ServiceUrls.Identity),
            Content = await fixture.NewContextAsync(ServiceUrls.Content),
            Progress = await fixture.NewContextAsync(ServiceUrls.Progress),
        };

        public async ValueTask DisposeAsync()
        {
            await Identity.DisposeAsync();
            await Content.DisposeAsync();
            await Progress.DisposeAsync();
        }
    }
}
