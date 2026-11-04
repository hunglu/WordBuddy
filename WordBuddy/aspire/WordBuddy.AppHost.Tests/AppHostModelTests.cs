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
        { "identity", ["ASPNETCORE_ENVIRONMENT", "OpenTelemetry__OtlpEndpoint", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer", "Jwt__ExpiryMinutes",
            "Messaging__RabbitMq__Host", "Messaging__RabbitMq__Username", "Messaging__RabbitMq__Password", "SupportLinks__UnlinkOverrideWaitDays"] },
        { "content", ["ASPNETCORE_ENVIRONMENT", "OpenTelemetry__OtlpEndpoint", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer", "FileStorage__BasePath",
            "Messaging__RabbitMq__Host", "Messaging__RabbitMq__Username", "Messaging__RabbitMq__Password",
            "Kestrel__Endpoints__Http__Url", "Kestrel__Endpoints__Grpc__Url"] },
        { "quiz", ["ASPNETCORE_ENVIRONMENT", "OpenTelemetry__OtlpEndpoint", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer"] },
        { "progress", ["ASPNETCORE_ENVIRONMENT", "OpenTelemetry__OtlpEndpoint", "ConnectionStrings__DefaultConnection", "Jwt__Secret", "Jwt__Issuer", "Services__Content__BaseUrl",
            "Services__Content__GrpcUrl", "Services__Content__Transport",
            "Messaging__RabbitMq__Host", "Messaging__RabbitMq__Username", "Messaging__RabbitMq__Password"] },
        { "notification", ["ASPNETCORE_ENVIRONMENT", "OpenTelemetry__OtlpEndpoint", "Jwt__Secret", "Jwt__Issuer"] },
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
            // Content's endpoint names come from its Kestrel config ("Http"), so compare without case.
            EndpointAnnotation http = resource.Annotations.OfType<EndpointAnnotation>().Single(e => string.Equals(e.Name, "http", StringComparison.OrdinalIgnoreCase));
            http.Port.Should().Be(port, "{0} must keep its compose port", name);
        }
    }

    [Fact]
    public async Task AppHost_Build_ContentHasGrpcEndpointOn5091WithHttp2()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource content = builder.Resources.Single(r => r.Name == "content");

        EndpointAnnotation grpc = content.Annotations.OfType<EndpointAnnotation>().Single(e => string.Equals(e.Name, "grpc", StringComparison.OrdinalIgnoreCase));
        EndpointAnnotation http = content.Annotations.OfType<EndpointAnnotation>().Single(e => string.Equals(e.Name, "http", StringComparison.OrdinalIgnoreCase));

        grpc.Port.Should().Be(5091);
        grpc.Transport.Should().Be("http2");
        grpc.UriScheme.Should().Be("http");
        http.Port.Should().Be(5081, "Content REST keeps its port");
    }

    [Fact]
    public async Task AppHost_Build_ProgressUsesGrpcTransportAndOnlyContentHasGrpcEndpoint()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource progress = builder.Resources.Single(r => r.Name == "progress");

        Dictionary<string, object> env = await AppHostFixture.GetEnvironmentAsync(progress);

        env["Services__Content__Transport"].Should().Be("Grpc");
        foreach (string api in Apis.Where(a => a != "content"))
        {
            builder.Resources.Single(r => r.Name == api).Annotations.OfType<EndpointAnnotation>()
                .Should().NotContain(e => string.Equals(e.Name, "grpc", StringComparison.OrdinalIgnoreCase), "{0} has no gRPC endpoint", api);
        }
    }

    [Fact]
    public async Task AppHost_Build_InfrastructurePortsArePinned()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();

        (string Resource, string Endpoint, int Port)[] expected =
        [
            ("sql", "tcp", 14330),
            ("rabbitmq", "tcp", 5672),
            ("rabbitmq", "management", 15672),
        ];

        foreach ((string name, string endpointName, int port) in expected)
        {
            IResource resource = builder.Resources.Single(r => r.Name == name);
            EndpointAnnotation endpoint = resource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == endpointName);
            endpoint.Port.Should().Be(port, "{0}/{1} must keep a fixed host port", name, endpointName);
        }
    }

    [Fact]
    public async Task AppHost_Build_NonSecretParameters_ResolveDefaults()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();

        Dictionary<string, string> expected = new()
        {
            ["jwt-issuer"] = "WordBuddy",
            ["jwt-expiry-minutes"] = "60",
            ["rabbitmq-user"] = "wordbuddy",
        };

        foreach ((string name, string value) in expected)
        {
            ParameterResource parameter = builder.Resources.OfType<ParameterResource>().Single(r => r.Name == name);
            parameter.Secret.Should().BeFalse("{0} is not a secret", name);
            (await parameter.GetValueAsync(CancellationToken.None)).Should().Be(value, "{0} must default from appsettings.json", name);
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

    public static TheoryData<string> ApiNames => [.. Apis];

    [Theory]
    [MemberData(nameof(ApiNames))]
    public async Task AppHost_Build_GrpcDashboard_OtlpEndpointMatchesInjectedEndpoint(string api)
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource resource = builder.Resources.Single(r => r.Name == api);

        Dictionary<string, object> env = await AppHostFixture.GetEnvironmentAsync(resource);

        env.Should().ContainKey("OTEL_EXPORTER_OTLP_ENDPOINT");
        env["OpenTelemetry__OtlpEndpoint"].Should().Be(env["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    }

    [Fact]
    public async Task AppHost_Build_HttpProtobufStringEndpoint_OtlpEndpointHasTracesPath()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource resource = builder.Resources.Single(r => r.Name == "identity");

        // The testing builder cannot select HTTP (see AppHostFixture.GrpcOtlpArg), so switch the injected values.
        Dictionary<string, object> env = await AppHostFixture.GetEnvironmentAsync(resource, e =>
        {
            if (e.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT"))
            {
                e["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:18890/";
                e["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf";
            }
        });

        env["OpenTelemetry__OtlpEndpoint"].Should().Be("http://localhost:18890/v1/traces");
    }

    [Fact]
    public async Task AppHost_Build_HttpProtobufProviderEndpoint_OtlpEndpointHasTracesPath()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        IResource resource = builder.Resources.Single(r => r.Name == "identity");

        // Aspire 13.6.4 injects an internal HostUrl provider (not a string); keep it and only switch the protocol.
        Dictionary<string, object> env = await AppHostFixture.GetEnvironmentAsync(resource, e =>
        {
            if (e.ContainsKey("OTEL_EXPORTER_OTLP_ENDPOINT"))
            {
                e["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf";
            }
        });

        IValueProvider provider = env["OpenTelemetry__OtlpEndpoint"].Should().BeAssignableTo<IValueProvider>().Subject;
        (await provider.GetValueAsync(CancellationToken.None)).Should().Be("http://localhost:18889/v1/traces");
    }

    [Fact]
    public async Task AppHost_Build_NoInjectedOtlpEndpoint_NoOtlpEndpointKey()
    {
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();

        foreach (string api in Apis)
        {
            IResource resource = builder.Resources.Single(r => r.Name == api);

            // Simulates a run without dashboard OTLP: drop what Aspire injected before the AppHost callback runs.
            Dictionary<string, object> env = await AppHostFixture.GetEnvironmentAsync(resource, e => e.Remove("OTEL_EXPORTER_OTLP_ENDPOINT"));

            env.Should().NotContainKey("OpenTelemetry__OtlpEndpoint", "{0} must not get a broken endpoint without a dashboard", api);
        }
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
