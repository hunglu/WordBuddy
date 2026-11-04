using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Extensions;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class AddContentClientTests
{
    private static IContentSenseClient Resolve(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddSingleton(TimeProvider.System);
        ServiceCollectionExtensions.AddContentClient(services, configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.CreateScope().ServiceProvider.GetRequiredService<IContentSenseClient>();
    }

    [Fact]
    public void AddContentClient_NoTransportSet_ResolvesGrpcClient()
    {
        Resolve([]).Should().BeOfType<ContentSenseGrpcClient>();
    }

    [Theory]
    [InlineData("Grpc")]
    [InlineData("grpc")]
    public void AddContentClient_TransportGrpc_ResolvesGrpcClient(string value)
    {
        Resolve(new() { ["Services:Content:Transport"] = value }).Should().BeOfType<ContentSenseGrpcClient>();
    }

    [Fact]
    public void AddContentClient_TransportRest_ResolvesRestClient()
    {
        Resolve(new() { ["Services:Content:Transport"] = "Rest" }).Should().BeOfType<ContentSenseClient>();
    }

    [Fact]
    public void AddContentClient_UnknownTransport_FailsFastAtStartup()
    {
        Action act = () => Resolve(new() { ["Services:Content:Transport"] = "Carrier" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*Transport*");
    }
}
