using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Contracts.Profiles;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>WB-20 on real SQL Server: date of birth, display name, password change, the extended
/// <c>me</c> and support-link views, and the no-date-of-birth guarantee for tokens and events.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class ProfileSettingsEndpointsTests
{
    private const string Password = "Passw0rd!";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IdentityApiFactory _factory;

    public ProfileSettingsEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Account(HttpClient Client, AuthTokenDto Auth, string Email);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<Account> RegisterAsync(string ageGroup, string name)
    {
        string email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { email, password = Password, displayName = $"{name} Real", ageGroup }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return new Account(Authorized(auth.Token), auth, email);
    }

    private HttpClient Authorized(string token)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string? Claim(string token, string type) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.FirstOrDefault(c => c.Type == type)?.Value;

    private static Task<HttpResponseMessage> PutDobAsync(HttpClient client, DateOnly? dob) =>
        client.PutAsJsonAsync("/api/auth/profile/date-of-birth", new { dateOfBirth = dob?.ToString("yyyy-MM-dd") });

    private static async Task<AuthTokenDto> ReadAuthAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;

    [Fact]
    public async Task DateOfBirthEndpoint_Put_AdultSavesAndTokenCarriesBandOnly()
    {
        Account adult = await RegisterAsync("Adult", "DobAdult");
        Claim(adult.Auth.Token, "age_band").Should().BeNull();
        DateOnly dob = Today.AddYears(-30);

        HttpResponseMessage response = await PutDobAsync(adult.Client, dob);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokenDto auth = await ReadAuthAsync(response);
        auth.User.DateOfBirth.Should().Be(dob);
        auth.User.AgeBand.Should().Be(AgeBand.Adult);
        Claim(auth.Token, "age_band").Should().Be("Adult");
        new JwtSecurityTokenHandler().ReadJwtToken(auth.Token).Claims
            .Should().NotContain(c => c.Value.Contains(dob.ToString("yyyy-MM-dd")));

        UserDto me = (await Authorized(auth.Token).GetFromJsonAsync<UserDto>("/api/auth/me", Json))!;
        me.DateOfBirth.Should().Be(dob);
        me.AgeBand.Should().Be(AgeBand.Adult);
        me.Email.Should().Be(adult.Email);
    }

    [Fact]
    public async Task DateOfBirthEndpoint_Put_ChildGetsChildBand()
    {
        Account child = await RegisterAsync("Child", "DobKid");

        HttpResponseMessage response = await PutDobAsync(child.Client, Today.AddYears(-8));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Claim((await ReadAuthAsync(response)).Token, "age_band").Should().Be("Age7To9");
    }

    [Fact]
    public async Task DateOfBirthEndpoint_Put_NullClearsAndDropsClaim()
    {
        Account adult = await RegisterAsync("Adult", "DobClear");
        (await PutDobAsync(adult.Client, Today.AddYears(-30))).EnsureSuccessStatusCode();

        HttpResponseMessage response = await PutDobAsync(adult.Client, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokenDto auth = await ReadAuthAsync(response);
        auth.User.DateOfBirth.Should().BeNull();
        Claim(auth.Token, "age_band").Should().BeNull();
    }

    [Fact]
    public async Task DateOfBirthEndpoint_Put_FutureDateIsBadRequest()
    {
        Account adult = await RegisterAsync("Adult", "DobFuture");

        (await PutDobAsync(adult.Client, Today.AddDays(1))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DateOfBirthEndpoint_Put_ConflictWithAgeGroupIsBadRequest()
    {
        Account child = await RegisterAsync("Child", "DobMismatchKid");
        Account adult = await RegisterAsync("Adult", "DobMismatchAdult");

        (await PutDobAsync(child.Client, Today.AddYears(-30))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PutDobAsync(adult.Client, Today.AddYears(-10))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await child.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!.DateOfBirth.Should().BeNull();
    }

    [Fact]
    public async Task ProfileSettingsEndpoints_WithoutToken_Return401()
    {
        HttpClient anonymous = _factory.CreateClient();

        (await PutDobAsync(anonymous, Today.AddYears(-30))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PutAsJsonAsync("/api/auth/profile/display-name", new { displayName = "Someone" })).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/auth/profile/password", new { currentPassword = Password, newPassword = "Other1234!" })).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DisplayNameEndpoint_Put_SavesAndMeShowsIt()
    {
        Account adult = await RegisterAsync("Adult", "NameAdult");

        HttpResponseMessage response = await adult.Client.PutAsJsonAsync("/api/auth/profile/display-name", new { displayName = "  New Name  " });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.DisplayName.Should().Be("New Name");
        (await adult.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task DisplayNameEndpoint_Put_TooShortIsBadRequest()
    {
        Account child = await RegisterAsync("Child", "NameKid");

        (await child.Client.PutAsJsonAsync("/api/auth/profile/display-name", new { displayName = "A" })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PasswordEndpoint_Post_ChangesPasswordAndOldOneStopsWorking()
    {
        Account adult = await RegisterAsync("Adult", "PwAdult");
        const string newPassword = "Brand-new-1";

        HttpResponseMessage response = await adult.Client.PostAsJsonAsync(
            "/api/auth/profile/password", new { currentPassword = Password, newPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAuthAsync(response)).Token.Should().NotBeNullOrEmpty();

        HttpClient anonymous = _factory.CreateClient();
        (await anonymous.PostAsJsonAsync("/api/auth/login", new { email = adult.Email, password = Password })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        (await anonymous.PostAsJsonAsync("/api/auth/login", new { email = adult.Email, password = newPassword })).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PasswordEndpoint_Post_ChildCanChangePassword()
    {
        Account child = await RegisterAsync("Child", "PwKid");

        (await child.Client.PostAsJsonAsync("/api/auth/profile/password", new { currentPassword = Password, newPassword = "Kid-new-pass1" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PasswordEndpoint_Post_WrongCurrentPasswordIsBadRequestAndPasswordUnchanged()
    {
        Account adult = await RegisterAsync("Adult", "PwWrong");

        HttpResponseMessage response = await adult.Client.PostAsJsonAsync(
            "/api/auth/profile/password", new { currentPassword = "not-my-password", newPassword = "Brand-new-1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = adult.Email, password = Password })).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PasswordEndpoint_Post_ShortNewPasswordIsBadRequest()
    {
        Account adult = await RegisterAsync("Adult", "PwShort");

        (await adult.Client.PostAsJsonAsync("/api/auth/profile/password", new { currentPassword = Password, newPassword = "short" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SupportLinks_LearnerDateOfBirth_VisibleToActiveSupporterOnly()
    {
        Account child = await RegisterAsync("Child", "DobLinkKid");
        Account parent = await RegisterAsync("Adult", "DobLinkParent");
        Account teacher = await RegisterAsync("Adult", "DobLinkTeacher");
        DateOnly dob = Today.AddYears(-9);

        // Parent becomes the Primary supporter (Active); teacher stays pending.
        (await AcceptAsync(parent.Client, (await InviteAsync(child.Client)).Code)).Status.Should().Be(SupportLinkStatus.Active);
        (await AcceptAsync(teacher.Client, (await InviteAsync(child.Client)).Code)).Status.Should().Be(SupportLinkStatus.PendingPrimaryApproval);
        (await PutDobAsync(child.Client, dob)).EnsureSuccessStatusCode();

        MySupportLinksDto parentView = (await parent.Client.GetFromJsonAsync<MySupportLinksDto>("/api/auth/support-links", Json))!;
        parentView.AsSupporter.Should().ContainSingle().Which.LearnerDateOfBirth.Should().Be(dob);

        MySupportLinksDto teacherView = (await teacher.Client.GetFromJsonAsync<MySupportLinksDto>("/api/auth/support-links", Json))!;
        teacherView.AsSupporter.Should().ContainSingle().Which.LearnerDateOfBirth.Should().BeNull();

        // The learner and the supporter have no endpoint to change the learner date of birth.
        MySupportLinksDto childView = (await child.Client.GetFromJsonAsync<MySupportLinksDto>("/api/auth/support-links", Json))!;
        childView.AsLearner.Should().OnlyContain(l => l.LearnerDateOfBirth == null);
        (await PutDobAsync(parent.Client, Today.AddYears(-40))).EnsureSuccessStatusCode();
        (await child.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!.DateOfBirth.Should().Be(dob);
    }

    [Fact]
    public async Task ProfileUpdatedEvent_AfterDateOfBirthAndNameChange_HasNoDateOfBirthAndNoChildName()
    {
        ConcurrentBag<LearnerProfileUpdated> messages = [];
        IBus bus = _factory.Services.GetRequiredService<IBus>();
        HostReceiveEndpointHandle endpoint = bus.ConnectReceiveEndpoint($"identity-profile-tests-{Guid.NewGuid():N}", e =>
        {
            e.Handler<LearnerProfileUpdated>(context =>
            {
                messages.Add(context.Message);
                return Task.CompletedTask;
            });
        });
        await endpoint.Ready;

        try
        {
            Account child = await RegisterAsync("Child", "EventKid");
            DateOnly dob = Today.AddYears(-8);
            (await PutDobAsync(child.Client, dob)).EnsureSuccessStatusCode();
            (await child.Client.PutAsJsonAsync("/api/auth/profile/display-name", new { displayName = "Secret Kid Name" })).EnsureSuccessStatusCode();

            Guid childId = child.Auth.User.Id;
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && messages.Count(m => m.UserId == childId) < 2)
            {
                await Task.Delay(100);
            }

            List<LearnerProfileUpdated> mine = messages.Where(m => m.UserId == childId).ToList();
            mine.Should().HaveCountGreaterThanOrEqualTo(2);
            mine.Should().OnlyContain(m => m.DisplayName == null);
            foreach (LearnerProfileUpdated message in mine)
            {
                string payload = JsonSerializer.Serialize(message);
                payload.Should().NotContain(dob.ToString("yyyy-MM-dd")).And.NotContain("Secret Kid Name");
            }

            typeof(LearnerProfileUpdated).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Birth"));
        }
        finally
        {
            await endpoint.StopAsync();
        }
    }

    private static async Task<CreatedInvitationDto> InviteAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/support-links/invitations", new { inviteAs = "Learner", relationship = "Parent" }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedInvitationDto>(Json))!;
    }

    private static async Task<SupportLinkDto> AcceptAsync(HttpClient client, string code)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/support-links/accept", new { code }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SupportLinkDto>(Json))!;
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task LanguageEndpoint_Put_SavesAndMeAndLoginReturnIt(string ageGroup)
    {
        Account account = await RegisterAsync(ageGroup, "Lang" + ageGroup);

        HttpResponseMessage response = await account.Client.PutAsJsonAsync("/api/auth/profile/language", new { language = "VI" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.PreferredLanguage.Should().Be("vi");
        UserDto me = (await account.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!;
        me.PreferredLanguage.Should().Be("vi");

        HttpResponseMessage login = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = account.Email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAuthAsync(login)).User.PreferredLanguage.Should().Be("vi");
    }

    [Fact]
    public async Task LanguageEndpoint_Put_NullClearsTheLanguage()
    {
        Account account = await RegisterAsync("Adult", "LangClear");
        await account.Client.PutAsJsonAsync("/api/auth/profile/language", new { language = "vi" });

        HttpResponseMessage response = await account.Client.PutAsJsonAsync("/api/auth/profile/language", new { language = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.PreferredLanguage.Should().BeNull();
    }

    [Theory]
    [InlineData("english")]
    [InlineData("en_US")]
    [InlineData("abcdefghijkl")]
    public async Task LanguageEndpoint_Put_MalformedCodeReturnsBadRequest(string language)
    {
        Account account = await RegisterAsync("Adult", "LangBad");

        HttpResponseMessage response = await account.Client.PutAsJsonAsync("/api/auth/profile/language", new { language });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LanguageEndpoint_Put_AnonymousReturnsUnauthorized()
    {
        HttpResponseMessage response = await _factory.CreateClient().PutAsJsonAsync("/api/auth/profile/language", new { language = "vi" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterEndpoint_Post_StoresPreferredLanguage()
    {
        string email = $"langreg-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { email, password = Password, displayName = "Lang Reg", ageGroup = "Adult", preferredLanguage = "vi" }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadAuthAsync(response)).User.PreferredLanguage.Should().Be("vi");
    }
}
