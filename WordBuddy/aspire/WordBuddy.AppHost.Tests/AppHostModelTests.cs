using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace WordBuddy.AppHost.Tests;

public class AppHostModelTests
{
    private static readonly string[] Apis = ["identity", "content", "quiz", "progress", "notification"];

    private static readonly string[] Databases = ["identitydb", "contentdb", "quizdb", "progressdb", "notificationdb"];

    // Keys mirror docker-compose.yml (plan env table). Values are never asserted.
    public static TheoryData<string, string[]> ExpectedKeys => new()
    {
        { "identity", ["ASPNETCORE_ENVIRONMENT", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer", "Jwt__ExpiryMinutes",
            "Messaging__RabbitMq__Host", "Messaging__RabbitMq__Username", "Messaging__RabbitMq__Password", "SupportLinks__UnlinkOverrideWaitDays"] },
        { "content", ["ASPNETCORE_ENVIRONMENT", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer", "FileStorage__BasePath",
            "Messaging__RabbitMq__Host", "Messaging__RabbitMq__Username", "Messaging__RabbitMq__Password"] },
        { "quiz", ["ASPNETCORE_ENVIRONMENT", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer"] },
        { "progress", ["ASPNETCORE_ENVIRONMENT", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer", "Services__Content__BaseUrl",
            "Messaging__RabbitMq__Host", "Messaging__RabbitMq__Username", "Messaging__RabbitMq__Password"] },
        { "notification", ["ASPNETCORE_ENVIRONMENT", "Jwt__Secret", "Jwt__Issuer"] },
    };

    [Fact]
    public async Task AppHost_Build_ContainsAllResources()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();

        string[] names = [.. builder.Resources.Select(r => r.Name)];

        names.Should().Contain(["sql", "rabbitmq", "ui"]);
        names.Should().Contain(Databases);
        names.Should().Contain(Apis);
    }

    [Fact]
    public async Task AppHost_Build_ApiEndpointsUseComposePorts()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();

        Dictionary<string, int> expected = new()
        {
            ["identity"] = 5080,
            ["content"] = 5081,
            ["quiz"] = 5082,
            ["progress"] = 5083,
            ["notification"] = 5084,
            ["ui"] = 3000,
        };

        foreach ((string name, int port) in expected)
        {
            IResource resource = builder.Resources.Single(r => r.Name == name);
            EndpointAnnotation http = resource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "http");
            http.Port.Should().Be(port, "{0} must keep its compose port", name);
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedKeys))]
    public async Task AppHost_Build_ApiEnvironmentMatchesComposeKeys(string api, string[] expectedKeys)
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource resource = builder.Resources.Single(r => r.Name == api);

        HashSet<string> env = await AppHostFixture.GetEnvironmentKeysAsync(resource);

        // Only WordBuddy config keys are compared; Aspire adds LOGGING__*, services__* etc.
        string[] wordBuddyKeys =
        [
            .. env
                .Where(k => k.Contains("__") && !k.StartsWith("services__", StringComparison.Ordinal) && !k.StartsWith("LOGGING__", StringComparison.Ordinal))
                .Concat(env.Where(k => k == "ASPNETCORE_ENVIRONMENT"))
                .Distinct(),
        ];
        wordBuddyKeys.Should().BeEquivalentTo(expectedKeys);
    }

    [Fact]
    public async Task AppHost_Build_ClaudeApiKeyUnset_ContentHasNoAutofillKey()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource content = builder.Resources.Single(r => r.Name == "content");

        HashSet<string> env = await AppHostFixture.GetEnvironmentKeysAsync(content);

        env.Should().NotContain("Autofill__Claude__ApiKey");
    }

    [Fact]
    public async Task AppHost_Build_ClaudeApiKeySet_ContentHasAutofillKey()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync("Parameters:claude-api-key=test-only-claude");
        IResource content = builder.Resources.Single(r => r.Name == "content");

        HashSet<string> env = await AppHostFixture.GetEnvironmentKeysAsync(content);

        env.Should().Contain("Autofill__Claude__ApiKey");
    }
}
