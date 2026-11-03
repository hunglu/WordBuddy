using Aspire.Hosting.ApplicationModel;

namespace WordBuddy.AppHost;

/// <summary>
/// Local dev orchestration for WordBuddy (ADR 0007). Starts SQL Server and RabbitMQ as containers,
/// the 5 APIs as processes (wired by csproj path, never by project reference) and the Vite UI.
/// Environment keys mirror <c>docker-compose.yml</c> so both run paths stay aligned.
/// </summary>
public static class Program
{
    /// <summary>Relative path from the AppHost folder to the services root.</summary>
    private const string ServicesRoot = "../../src/Services";

    /// <summary>Fixed SQL Server host port; compose owns 1433.</summary>
    public const int SqlPort = 14330;

    /// <summary>Entry point: builds the model and runs it.</summary>
    /// <param name="args">Command-line arguments passed to the distributed application builder.</param>
    public static async Task Main(string[] args)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
        ConfigureModel(builder);
        await builder.Build().RunAsync();
    }

    /// <summary>Adds every WordBuddy resource to the given builder.</summary>
    /// <param name="builder">The distributed application builder.</param>
    public static void ConfigureModel(IDistributedApplicationBuilder builder)
    {
        // Parameters: secrets live in AppHost user-secrets as Parameters:<name> (never in git);
        // non-secret defaults live in appsettings.json under Parameters.
        IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-password", secret: true);
        IResourceBuilder<ParameterResource> jwtSecret = builder.AddParameter("jwt-secret", secret: true);
        IResourceBuilder<ParameterResource> rabbitPassword = builder.AddParameter("rabbitmq-password", secret: true);
        IResourceBuilder<ParameterResource> jwtIssuer = builder.AddParameter("jwt-issuer");
        IResourceBuilder<ParameterResource> jwtExpiry = builder.AddParameter("jwt-expiry-minutes");
        IResourceBuilder<ParameterResource> rabbitUser = builder.AddParameter("rabbitmq-user");

        // Optional: unset = Autofill AI off (same as today).
        IResourceBuilder<ParameterResource>? claudeApiKey = string.IsNullOrWhiteSpace(builder.Configuration["Parameters:claude-api-key"])
            ? null
            : builder.AddParameter("claude-api-key", secret: true);

        // Infrastructure containers. SQL host port is pinned to 14330 (not compose's 1433, so the persistent
        // container never blocks `make up`); RabbitMQ AMQP is pinned (no Port option in the services), so it
        // uses the session lifetime and frees 5672/15672 on stop.
        IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql", sqlPassword, port: SqlPort)
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent);

        IResourceBuilder<SqlServerDatabaseResource> identityDb = sql.AddDatabase("identitydb", "WordBuddyIdentity");
        IResourceBuilder<SqlServerDatabaseResource> contentDb = sql.AddDatabase("contentdb", "WordBuddyContent");
        IResourceBuilder<SqlServerDatabaseResource> quizDb = sql.AddDatabase("quizdb", "WordBuddyQuiz");
        IResourceBuilder<SqlServerDatabaseResource> progressDb = sql.AddDatabase("progressdb", "WordBuddyProgress");
        sql.AddDatabase("notificationdb", "WordBuddyNotification");

        IResourceBuilder<RabbitMQServerResource> rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPassword, port: 5672)
            .WithManagementPlugin(port: 15672)
            .WithDataVolume();

        // APIs: launch profiles are ignored; ports are pinned to the compose / Vite proxy / e2e ports.
        IResourceBuilder<ProjectResource> identity = AddApi(builder, "identity", "Identity", 5080)
            .WithReference(identityDb, "DefaultConnection").WaitFor(identityDb)
            .WithJwt(jwtSecret, jwtIssuer)
            .WithEnvironment("Jwt__ExpiryMinutes", jwtExpiry)
            .WithRabbitMq(rabbitmq, rabbitUser, rabbitPassword)
            .WithEnvironment("SupportLinks__UnlinkOverrideWaitDays", "7");

        string mediaPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".data", "media"));
        IResourceBuilder<ProjectResource> content = AddApi(builder, "content", "Content", 5081)
            .WithReference(contentDb, "DefaultConnection").WaitFor(contentDb)
            .WithJwt(jwtSecret, jwtIssuer)
            .WithEnvironment("FileStorage__BasePath", mediaPath)
            .WithRabbitMq(rabbitmq, rabbitUser, rabbitPassword);
        if (claudeApiKey is not null)
        {
            content.WithEnvironment("Autofill__Claude__ApiKey", claudeApiKey);
        }

        IResourceBuilder<ProjectResource> quiz = AddApi(builder, "quiz", "Quiz", 5082)
            .WithReference(quizDb, "DefaultConnection").WaitFor(quizDb)
            .WithJwt(jwtSecret, jwtIssuer);

        IResourceBuilder<ProjectResource> progress = AddApi(builder, "progress", "Progress", 5083)
            .WithReference(progressDb, "DefaultConnection").WaitFor(progressDb)
            .WithJwt(jwtSecret, jwtIssuer)
            .WithEnvironment("Services__Content__BaseUrl", content.GetEndpoint("http"))
            .WithRabbitMq(rabbitmq, rabbitUser, rabbitPassword);

        // Notification is a scaffold: JWT only, no database connection (same as compose).
        AddApi(builder, "notification", "Notification", 5084)
            .WithJwt(jwtSecret, jwtIssuer);

        // Vite UI on 3000. Its existing proxy reaches the APIs on their pinned ports.
        builder.AddViteApp("ui", "../../../WordBuddy.UI")
            .WithEndpoint("http", endpoint => endpoint.Port = 3000)
            .WaitFor(identity)
            .WaitFor(content)
            .WaitFor(quiz)
            .WaitFor(progress);
    }

    private static IResourceBuilder<ProjectResource> AddApi(
        IDistributedApplicationBuilder builder, string name, string service, int port)
    {
        string csproj = $"{ServicesRoot}/{service}/WordBuddy.{service}.Api/WordBuddy.{service}.Api.csproj";
        return builder.AddProject(name, csproj, launchProfileName: null)
            .WithHttpEndpoint(port: port, name: "http")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpHealthCheck("/health/ready")
            .WithDashboardOtlpEndpoint();
    }

    /// <summary>
    /// Copies the OTLP endpoint that Aspire injects (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>) into
    /// <c>OpenTelemetry__OtlpEndpoint</c>. The shared OTel helper reads <c>OpenTelemetry:OtlpEndpoint</c>
    /// first, so a local <c>appsettings.Development.json</c> value would otherwise override the injected
    /// endpoint and traces would miss the dashboard. An environment variable beats that file.
    /// For <c>http/protobuf</c> the exporter uses an endpoint set in code as is, so <c>/v1/traces</c> is appended.
    /// Does nothing when Aspire injects no endpoint (dashboard off).
    /// </summary>
    private static IResourceBuilder<ProjectResource> WithDashboardOtlpEndpoint(
        this IResourceBuilder<ProjectResource> project)
        => project.WithEnvironment(context =>
        {
            if (!context.EnvironmentVariables.TryGetValue("OTEL_EXPORTER_OTLP_ENDPOINT", out object? endpoint))
            {
                return;
            }

            context.EnvironmentVariables.TryGetValue("OTEL_EXPORTER_OTLP_PROTOCOL", out object? protocol);
            bool isHttp = string.Equals(protocol?.ToString(), "http/protobuf", StringComparison.OrdinalIgnoreCase);
            context.EnvironmentVariables["OpenTelemetry__OtlpEndpoint"] = isHttp ? WithTracesPath(endpoint) : endpoint;
        });

    private static object WithTracesPath(object endpoint)
        => endpoint switch
        {
            string text => text.TrimEnd('/') + "/v1/traces",
            // Aspire injects its own internal URL provider (not a string) in 13.x: wrap it, resolve at launch.
            IValueProvider and IManifestExpressionProvider => new TracesUrl(endpoint),
            _ => endpoint.ToString()!.TrimEnd('/') + "/v1/traces",
        };

    /// <summary>Wraps an injected endpoint value provider and appends <c>/v1/traces</c> when resolved.</summary>
    private sealed class TracesUrl(object inner) : IValueProvider, IManifestExpressionProvider
    {
        private const string Path = "/v1/traces";

        public string ValueExpression => ((IManifestExpressionProvider)inner).ValueExpression + Path;

        public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
        {
            string? value = await ((IValueProvider)inner).GetValueAsync(cancellationToken);
            return value?.TrimEnd('/') + Path;
        }

        public async ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken = default)
        {
            string? value = await ((IValueProvider)inner).GetValueAsync(context, cancellationToken);
            return value?.TrimEnd('/') + Path;
        }

        public override string ToString() => ValueExpression;
    }

    private static IResourceBuilder<ProjectResource> WithJwt(
        this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<ParameterResource> secret,
        IResourceBuilder<ParameterResource> issuer)
        => project.WithEnvironment("Jwt__Secret", secret).WithEnvironment("Jwt__Issuer", issuer);

    private static IResourceBuilder<ProjectResource> WithRabbitMq(
        this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ParameterResource> user,
        IResourceBuilder<ParameterResource> password)
        => project
            .WithEnvironment("Messaging__RabbitMq__Host", rabbitmq.GetEndpoint("tcp").Property(EndpointProperty.Host))
            .WithEnvironment("Messaging__RabbitMq__Username", user)
            .WithEnvironment("Messaging__RabbitMq__Password", password)
            .WaitFor(rabbitmq);
}
