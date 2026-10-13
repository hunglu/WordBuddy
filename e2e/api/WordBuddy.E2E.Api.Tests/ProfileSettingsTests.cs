using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-20: profile settings on Identity. Date of birth (child and adult), password change, and the
/// read-only date of birth view for an Active supporter. The token carries only the age band.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class ProfileSettingsTests
{
    private const string Password = "ChangeMe123!";
    private const string NewPassword = "NewSecret456!";

    private readonly ApiRequestContextFixture _fixture;

    public ProfileSettingsTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DateOfBirth_ChildAged8_IsStoredAndTokenCarriesBandOnly()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, _, _) = await RegisterAsync(identity, "Child");
        string dob = DateOnly.FromDateTime(DateTime.Today).AddYears(-8).AddDays(-10).ToString("yyyy-MM-dd");

        IAPIResponse response = await PutDateOfBirthAsync(identity, token, dob);

        response.Status.Should().Be(200, await response.TextAsync());
        JsonElement body = (await response.JsonAsync())!.Value;
        string newToken = body.GetProperty("token").GetString()!;
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(newToken);
        jwt.Claims.Single(c => c.Type == "age_band").Value.Should().Be("Age7To9");
        jwt.Claims.Should().NotContain(c => c.Value.Contains(dob), "the date of birth must not be in the token");

        IAPIResponse me = await identity.GetAsync("/api/auth/me", Options(newToken));
        me.Status.Should().Be(200);
        JsonElement user = (await me.JsonAsync())!.Value;
        user.GetProperty("dateOfBirth").GetString().Should().Be(dob);
        user.GetProperty("ageBand").GetString().Should().Be("Age7To9");
    }

    [Fact]
    public async Task DateOfBirth_AdultAged30_GetsAdultBand()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, _, _) = await RegisterAsync(identity, "Adult");
        string dob = DateOnly.FromDateTime(DateTime.Today).AddYears(-30).ToString("yyyy-MM-dd");

        IAPIResponse response = await PutDateOfBirthAsync(identity, token, dob);

        response.Status.Should().Be(200, await response.TextAsync());
        string newToken = (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
        new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims
            .Single(c => c.Type == "age_band").Value.Should().Be("Adult");
    }

    [Fact]
    public async Task DateOfBirth_InTheFuture_Returns400()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, _, _) = await RegisterAsync(identity, "Adult");
        string future = DateOnly.FromDateTime(DateTime.Today).AddYears(1).ToString("yyyy-MM-dd");

        IAPIResponse response = await PutDateOfBirthAsync(identity, token, future);

        response.Status.Should().Be(400, await response.TextAsync());
    }

    [Fact]
    public async Task DateOfBirth_AdultDateOnChildAccount_Returns400()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, _, _) = await RegisterAsync(identity, "Child");

        IAPIResponse response = await PutDateOfBirthAsync(identity, token, "1980-01-01");

        response.Status.Should().Be(400, await response.TextAsync());
    }

    [Fact]
    public async Task DateOfBirth_WithoutToken_Returns401()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);

        IAPIResponse response = await identity.PutAsync("/api/auth/profile/date-of-birth", new APIRequestContextOptions
        {
            DataObject = new { dateOfBirth = "1990-01-01" },
        });

        response.Status.Should().Be(401);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, _, _) = await RegisterAsync(identity, "Adult");

        IAPIResponse response = await PostPasswordAsync(identity, token, "WrongPassword999!", NewPassword);

        response.Status.Should().Be(400, await response.TextAsync());
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task ChangePassword_CorrectCurrentPassword_OldPasswordStopsWorking(string ageGroup)
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string token, string email, _) = await RegisterAsync(identity, ageGroup);

        IAPIResponse change = await PostPasswordAsync(identity, token, Password, NewPassword);

        change.Status.Should().Be(200, await change.TextAsync());
        (await LoginAsync(identity, email, Password)).Ok.Should().BeFalse("the old password must no longer log in");
        (await LoginAsync(identity, email, NewPassword)).Ok.Should().BeTrue("the new password must log in");
    }

    [Fact]
    public async Task SupportLinks_ActiveSupporter_SeesLearnerDateOfBirthAndCannotEditIt()
    {
        await using IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        (string childToken, _, Guid childId) = await RegisterAsync(identity, "Child");
        (string supporterToken, _, _) = await RegisterAsync(identity, "Adult");
        string dob = DateOnly.FromDateTime(DateTime.Today).AddYears(-9).ToString("yyyy-MM-dd");
        (await PutDateOfBirthAsync(identity, childToken, dob)).Status.Should().Be(200);

        IAPIResponse invitation = await identity.PostAsync("/api/auth/support-links/invitations", new APIRequestContextOptions
        {
            Headers = Auth(childToken),
            DataObject = new { inviteAs = "Learner", relationship = (string?)null },
        });
        invitation.Status.Should().Be(201, await invitation.TextAsync());
        string code = (await invitation.JsonAsync())!.Value.GetProperty("code").GetString()!;
        IAPIResponse accept = await identity.PostAsync("/api/auth/support-links/accept", new APIRequestContextOptions
        {
            Headers = Auth(supporterToken),
            DataObject = new { code, token = (string?)null },
        });
        accept.Status.Should().Be(200, await accept.TextAsync());

        IAPIResponse links = await identity.GetAsync("/api/auth/support-links", Options(supporterToken));
        links.Status.Should().Be(200, await links.TextAsync());
        JsonElement asSupporter = (await links.JsonAsync())!.Value.GetProperty("asSupporter");
        JsonElement entry = asSupporter.EnumerateArray().Single();
        entry.GetProperty("status").GetString().Should().Be("Active");
        entry.GetProperty("learnerDateOfBirth").GetString().Should().Be(dob);

        // The supporter's own date of birth endpoint touches only the supporter's account, never the learner's.
        string other = DateOnly.FromDateTime(DateTime.Today).AddYears(-40).ToString("yyyy-MM-dd");
        (await PutDateOfBirthAsync(identity, supporterToken, other)).Status.Should().Be(200);
        IAPIResponse childMe = await identity.GetAsync("/api/auth/me", Options(childToken));
        (await childMe.JsonAsync())!.Value.GetProperty("dateOfBirth").GetString().Should().Be(dob);
        childId.Should().NotBeEmpty();
    }

    private static Task<IAPIResponse> PutDateOfBirthAsync(IAPIRequestContext identity, string token, string dob) =>
        identity.PutAsync("/api/auth/profile/date-of-birth", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { dateOfBirth = dob },
        });

    private static Task<IAPIResponse> PostPasswordAsync(IAPIRequestContext identity, string token, string current, string next) =>
        identity.PostAsync("/api/auth/profile/password", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { currentPassword = current, newPassword = next },
        });

    private static Task<IAPIResponse> LoginAsync(IAPIRequestContext identity, string email, string password) =>
        identity.PostAsync("/api/auth/login", new APIRequestContextOptions
        {
            DataObject = new { email, password },
        });

    private static async Task<(string Token, string Email, Guid UserId)> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        string email = $"profile-e2e-{Guid.NewGuid():N}@example.com";
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new { email, password = Password, displayName = "E2E Profile User", ageGroup },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return (body.GetProperty("token").GetString()!, email, body.GetProperty("user").GetProperty("id").GetGuid());
    }

    private static APIRequestContextOptions Options(string token) => new() { Headers = Auth(token) };

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
