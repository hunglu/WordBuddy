using Microsoft.Extensions.Configuration;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Resolves the base URL of each WordBuddy microservice for blackbox API testing.
/// Defaults match the docker-compose local-dev port mapping; override any of them via the
/// <c>Services__&lt;Service&gt;</c> environment variable (e.g. <c>Services__Identity</c>) to
/// point at a different environment (local Kubernetes, staging, ...).
/// </summary>
public static class ServiceUrls
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();

    public static string Identity => Get("Identity");

    public static string Content => Get("Content");

    public static string Quiz => Get("Quiz");

    public static string Progress => Get("Progress");

    public static string Notification => Get("Notification");

    /// <summary>The single public origin learners use (the UI's Nginx in docker-compose, the Ingress
    /// in Kubernetes) — <c>Services__Public</c> to override.</summary>
    public static string Public => Get("Public");

    private static string Get(string service)
    {
        return Configuration[$"Services:{service}"]
            ?? throw new InvalidOperationException($"No base URL configured for service '{service}'.");
    }
}
