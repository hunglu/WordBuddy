using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WordBuddy.Shared.Infrastructure;

namespace WordBuddy.Identity.UnitTests.Persistence;

/// <summary>
/// Covers the shared forwarded-headers helper (WordBuddy.Shared.Infrastructure 1.4.0). It lives here because the
/// shared packages have no test project of their own (ADR 0001: no shared code between services).
/// </summary>
public sealed class ForwardedHeadersExtensionsTests
{
    private static ForwardedHeadersOptions Resolve(params string[] networks)
    {
        Dictionary<string, string?> values = [];
        for (int i = 0; i < networks.Length; i++)
        {
            values[$"ForwardedHeaders:KnownNetworks:{i}"] = networks[i];
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        ServiceCollection services = new();
        services.AddWordBuddyForwardedHeaders(configuration);
        return services.BuildServiceProvider().GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Fact]
    public void ForwardedHeadersExtensions_AddWordBuddyForwardedHeaders_EmptyConfigTrustsNoProxy()
    {
        ForwardedHeadersOptions options = Resolve();

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.None);
    }

    [Fact]
    public void ForwardedHeadersExtensions_AddWordBuddyForwardedHeaders_ParsesCidrList()
    {
        ForwardedHeadersOptions options = Resolve("172.28.0.0/16", "10.0.0.0/8");

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        options.KnownIPNetworks.Should().HaveCount(2);
        options.KnownProxies.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-cidr")]
    [InlineData("172.28.0.0")]
    [InlineData("172.28.0.0/40")]
    public void ForwardedHeadersExtensions_AddWordBuddyForwardedHeaders_InvalidCidrFailsAtStartup(string entry)
    {
        Action act = () => Resolve(entry);

        act.Should().Throw<InvalidOperationException>();
    }
}
