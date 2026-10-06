using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Content.IntegrationTests.Messaging;

/// <summary>Unit tests of the shared <c>AddWordBuddyMessaging</c> registration (no broker, no database).</summary>
public sealed class MessagingRegistrationTests
{
    private static IServiceCollection Register(string? transport)
    {
        Dictionary<string, string?> settings = [];
        if (transport is not null)
        {
            settings[MessagingExtensions.TransportKey] = transport;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddWordBuddyMessaging<ContentDbContext>(configuration);
        return services;
    }

    [Fact]
    public void AddWordBuddyMessaging_InMemoryTransport_RegistersTestHarness()
    {
        IServiceCollection services = Register(MessagingExtensions.InMemoryTransport);

        services.Should().Contain(d => d.ServiceType == typeof(ITestHarness));
    }

    [Fact]
    public void AddWordBuddyMessaging_DefaultTransport_DoesNotRegisterTestHarness()
    {
        IServiceCollection services = Register(transport: null);

        services.Should().NotContain(d => d.ServiceType == typeof(ITestHarness));
    }

    [Fact]
    public void AddWordBuddyMessaging_UnknownTransport_Throws()
    {
        Action act = () => Register("Kafka");

        act.Should().Throw<InvalidOperationException>();
    }
}
