using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Content.UnitTests.Infrastructure;

/// <summary>Binding and validation of the shared <see cref="MessagingOptions"/>.</summary>
public sealed class MessagingOptionsTests
{
    private static MessagingOptions Bind(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        MessagingOptions options = new();
        configuration.GetSection(MessagingOptions.SectionName).Bind(options);
        return options;
    }

    private static MessagingOptionsValidator CreateValidator(string environment)
    {
        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns(environment);
        env.SetupGet(e => e.ContentRootFileProvider).Returns(new NullFileProvider());
        return new MessagingOptionsValidator(env.Object);
    }

    private static MessagingOptions ValidRabbit()
    {
        MessagingOptions options = new();
        options.RabbitMq.Username = "user";
        options.RabbitMq.Password = "pw";
        options.Retry.ApplyDefaults();
        return options;
    }

    private static MessagingOptions ValidDefaults()
    {
        MessagingOptions options = new();
        options.Retry.ApplyDefaults();
        return options;
    }

    [Fact]
    public void MessagingOptions_BindFromConfiguration_ReadsAllValues()
    {
        MessagingOptions options = Bind(new Dictionary<string, string?>
        {
            ["Messaging:Transport"] = "InMemory",
            ["Messaging:RabbitMq:Host"] = "broker",
            ["Messaging:RabbitMq:VirtualHost"] = "vh",
            ["Messaging:RabbitMq:Username"] = "u",
            ["Messaging:RabbitMq:HeartbeatSeconds"] = "30",
            ["Messaging:RabbitMq:RequestedConnectionTimeoutMs"] = "5000",
            ["Messaging:Outbox:QueryDelayMs"] = "250",
            ["Messaging:Outbox:DuplicateDetectionWindowSeconds"] = "60",
            ["Messaging:Retry:IntervalsMs:0"] = "500",
            ["Messaging:Retry:IntervalsMs:1"] = "2500",
        });

        options.Transport.Should().Be("InMemory");
        options.RabbitMq.Host.Should().Be("broker");
        options.RabbitMq.VirtualHost.Should().Be("vh");
        options.RabbitMq.Username.Should().Be("u");
        options.RabbitMq.HeartbeatSeconds.Should().Be(30);
        options.RabbitMq.RequestedConnectionTimeoutMs.Should().Be(5000);
        options.Outbox.QueryDelayMs.Should().Be(250);
        options.Outbox.DuplicateDetectionWindowSeconds.Should().Be(60);
        options.Retry.IntervalsMs.Should().Equal(500, 2500);
    }

    [Fact]
    public void MessagingOptions_AppsettingsStyleThreeItems_BindsExactlyThree()
    {
        MessagingOptions options = Bind(new Dictionary<string, string?>
        {
            ["Messaging:Retry:IntervalsMs:0"] = "200",
            ["Messaging:Retry:IntervalsMs:1"] = "1000",
            ["Messaging:Retry:IntervalsMs:2"] = "5000",
        });
        options.Retry.ApplyDefaults();

        options.Retry.IntervalsMs.Should().Equal(200, 1000, 5000);
    }

    [Fact]
    public void MessagingOptions_OverrideWithFewerItems_BindsExactlyTheOverride()
    {
        MessagingOptions options = Bind(new Dictionary<string, string?>
        {
            ["Messaging:Retry:IntervalsMs:0"] = "500",
        });
        options.Retry.ApplyDefaults();

        options.Retry.IntervalsMs.Should().Equal(500);
    }

    [Fact]
    public void MessagingOptions_SectionMissing_UsesDefaults()
    {
        MessagingOptions options = Bind([]);
        options.Retry.ApplyDefaults();

        options.Transport.Should().Be("RabbitMq");
        options.RabbitMq.Host.Should().Be("localhost");
        options.RabbitMq.VirtualHost.Should().Be("/");
        options.RabbitMq.HeartbeatSeconds.Should().Be(10);
        options.RabbitMq.RequestedConnectionTimeoutMs.Should().Be(10000);
        options.Outbox.QueryDelayMs.Should().Be(1000);
        options.Outbox.DuplicateDetectionWindowSeconds.Should().Be(1800);
        options.Retry.IntervalsMs.Should().Equal(200, 1000, 5000);
    }

    [Fact]
    public void Validate_DefaultsWithCredentials_Succeeds()
    {
        ValidateOptionsResult result = CreateValidator("Production").Validate(null, ValidRabbit());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, "Messaging:Outbox:QueryDelayMs")]
    [InlineData(-5, "Messaging:Outbox:QueryDelayMs")]
    public void Validate_NonPositiveQueryDelay_FailsNamingKey(int value, string key)
    {
        MessagingOptions options = ValidRabbit();
        options.Outbox.QueryDelayMs = value;

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(key);
    }

    [Fact]
    public void Validate_NonPositiveDuplicateWindow_FailsNamingKey()
    {
        MessagingOptions options = ValidRabbit();
        options.Outbox.DuplicateDetectionWindowSeconds = 0;

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.FailureMessage.Should().Contain("Messaging:Outbox:DuplicateDetectionWindowSeconds");
    }

    [Fact]
    public void Validate_EmptyRetryList_FailsNamingKey()
    {
        MessagingOptions options = ValidRabbit();
        options.Retry.IntervalsMs = [];

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.FailureMessage.Should().Contain("Messaging:Retry:IntervalsMs");
    }

    [Fact]
    public void Validate_NegativeRetryInterval_FailsNamingKey()
    {
        MessagingOptions options = ValidRabbit();
        options.Retry.IntervalsMs = [200, -1];

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.FailureMessage.Should().Contain("Messaging:Retry:IntervalsMs");
    }

    [Fact]
    public void Validate_EmptyHost_FailsNamingKey()
    {
        MessagingOptions options = ValidRabbit();
        options.RabbitMq.Host = " ";

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.FailureMessage.Should().Contain("Messaging:RabbitMq:Host");
    }

    [Fact]
    public void Validate_BadHeartbeatAndTimeout_FailsNamingKeys()
    {
        MessagingOptions options = ValidRabbit();
        options.RabbitMq.HeartbeatSeconds = -1;
        options.RabbitMq.RequestedConnectionTimeoutMs = 0;

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.FailureMessage.Should().Contain("Messaging:RabbitMq:HeartbeatSeconds")
            .And.Contain("Messaging:RabbitMq:RequestedConnectionTimeoutMs");
    }

    [Fact]
    public void Validate_UnknownTransport_FailsNamingKey()
    {
        MessagingOptions options = ValidRabbit();
        options.Transport = "Kafka";

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.FailureMessage.Should().Contain("Messaging:Transport");
    }

    [Fact]
    public void Validate_MissingCredentialsInProduction_Fails()
    {
        ValidateOptionsResult result = CreateValidator("Production").Validate(null, ValidDefaults());

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Messaging:RabbitMq:Username")
            .And.Contain("Messaging:RabbitMq:Password");
    }

    [Fact]
    public void Validate_MissingCredentialsInDevelopment_Succeeds()
    {
        ValidateOptionsResult result = CreateValidator("Development").Validate(null, ValidDefaults());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingCredentialsWithInMemoryTransport_Succeeds()
    {
        MessagingOptions options = ValidDefaults();
        options.Transport = "InMemory";

        ValidateOptionsResult result = CreateValidator("Production").Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }
}
