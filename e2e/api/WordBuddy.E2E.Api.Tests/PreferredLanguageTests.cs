using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-59: the preferred UI language is saved on the profile and returned on the next login (any device).
/// Child and adult accounts behave the same.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class PreferredLanguageTests
{
    private const string Password = "ChangeMe123!";

    private readonly ApiRequestContextFixture _fixture;

    public PreferredLanguageTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task Language_SavedThenLoginAgain_ReplyCarriesIt(string ageGroup)
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, string email) = await RegisterAsync(identity, ageGroup);

        IAPIResponse save = await identity.PutAsync("/api/auth/profile/language", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { language = "vi" },
        });
        save.Status.Should().Be(200, await save.TextAsync());

        IAPIResponse login = await identity.PostAsync("/api/auth/login", new APIRequestContextOptions
        {
            DataObject = new { email, password = Password },
        });
        login.Status.Should().Be(200, await login.TextAsync());
        (await login.JsonAsync())!.Value.GetProperty("user").GetProperty("preferredLanguage").GetString().Should().Be("vi");
    }

    [Fact]
    public async Task Language_MalformedCode_Returns400()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, _) = await RegisterAsync(identity, "Adult");

        IAPIResponse save = await identity.PutAsync("/api/auth/profile/language", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { language = "english" },
        });

        save.Status.Should().Be(400, await save.TextAsync());
    }

    [Fact]
    public async Task Language_WithoutToken_Returns401()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);

        IAPIResponse save = await identity.PutAsync("/api/auth/profile/language", new APIRequestContextOptions
        {
            DataObject = new { language = "vi" },
        });

        save.Status.Should().Be(401);
    }

    private static async Task<(string Token, string Email)> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        string email = $"lang-e2e-{Guid.NewGuid():N}@example.com";
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new { email, password = Password, displayName = "E2E Language User", ageGroup },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return (body.GetProperty("token").GetString()!, email);
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
