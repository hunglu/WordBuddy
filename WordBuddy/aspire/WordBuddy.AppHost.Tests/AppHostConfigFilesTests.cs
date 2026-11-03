using System.Text.Json;
using FluentAssertions;

namespace WordBuddy.AppHost.Tests;

/// <summary>Guards the AppHost config files that decide how <c>aspire run</c> starts. No Docker needed.</summary>
public class AppHostConfigFilesTests
{
    private static readonly string[] SecretParameters = ["sql-password", "jwt-secret", "rabbitmq-password", "claude-api-key"];

    private static string AppHostFile(params string[] parts)
        => Path.Combine([AppHostFixture.FindBackendRoot(), "aspire", "WordBuddy.AppHost", .. parts]);

    private static List<(string Name, JsonElement Value)> Profiles()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(AppHostFile("Properties", "launchSettings.json")));
        // Clone so the elements outlive the disposed document.
        return [.. document.RootElement.GetProperty("profiles").EnumerateObject().Select(p => (p.Name, p.Value.Clone()))];
    }

    private static string Env((string Name, JsonElement Value) profile, string key)
        => profile.Value.GetProperty("environmentVariables").TryGetProperty(key, out JsonElement value)
            ? value.GetString() ?? string.Empty
            : string.Empty;

    [Fact]
    public void LaunchSettings_AllProfiles_UseDevelopmentEnvironment()
    {
        List<(string Name, JsonElement Value)> profiles = Profiles();

        profiles.Should().NotBeEmpty();
        foreach ((string Name, JsonElement Value) profile in profiles)
        {
            Env(profile, "ASPNETCORE_ENVIRONMENT").Should().Be("Development", "profile {0} must load user-secrets", profile.Name);
            Env(profile, "DOTNET_ENVIRONMENT").Should().Be("Development", "profile {0} must load user-secrets", profile.Name);
        }
    }

    [Fact]
    public void LaunchSettings_AllProfiles_UsePinnedDashboardPorts()
    {
        List<(string Name, JsonElement Value)> profiles = Profiles();

        profiles.Should().NotBeEmpty();
        foreach ((string Name, JsonElement Value) profile in profiles)
        {
            profile.Value.GetProperty("applicationUrl").GetString().Should().EndWith(":15180", "profile {0}", profile.Name);
            Env(profile, "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL").Should().EndWith(":19180", "profile {0}", profile.Name);
            Env(profile, "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL").Should().EndWith(":20180", "profile {0}", profile.Name);
        }
    }

    [Fact]
    public void AppHostProject_UserSecretsId_IsSet()
    {
        string csproj = File.ReadAllText(AppHostFile("WordBuddy.AppHost.csproj"));

        System.Text.RegularExpressions.Match match =
            System.Text.RegularExpressions.Regex.Match(csproj, @"<UserSecretsId>\s*(?<id>[^<\s]+)\s*</UserSecretsId>");
        match.Success.Should().BeTrue("the AppHost needs a UserSecretsId to load user-secrets");
        match.Groups["id"].Value.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("Properties/launchSettings.json")]
    public void ConfigFiles_SecretParameters_AreAbsent(string file)
    {
        string content = File.ReadAllText(AppHostFile(file.Split('/')));

        foreach (string secret in SecretParameters)
        {
            content.Should().NotContain(secret, "{0} must not hold the secret parameter {1}", file, secret);
        }
    }
}
