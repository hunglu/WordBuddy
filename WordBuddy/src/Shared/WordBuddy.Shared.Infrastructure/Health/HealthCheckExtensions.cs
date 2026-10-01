using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WordBuddy.Shared.Infrastructure.Health;

/// <summary>
/// Standard WordBuddy health endpoints: <c>GET /health</c> (liveness — the process is up and
/// serving requests, no dependency checks) and <c>GET /health/ready</c> (readiness — every check
/// tagged <see cref="ReadyTag"/>, e.g. the database, Redis, downstream services).
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Tag a service puts on a health check so it runs as part of <c>/health/ready</c>.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Liveness endpoint path.</summary>
    public const string LivenessPath = "/health";

    /// <summary>Readiness endpoint path.</summary>
    public const string ReadinessPath = "/health/ready";

    /// <summary>
    /// Registers the health-check services. Add dependency checks to the returned builder with
    /// <c>tags: [HealthCheckExtensions.ReadyTag]</c> so they are part of readiness.
    /// </summary>
    public static IHealthChecksBuilder AddWordBuddyHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks();
    }

    /// <summary>Maps <c>/health</c> (liveness) and <c>/health/ready</c> (readiness), both anonymous.</summary>
    public static IEndpointRouteBuilder MapWordBuddyHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = _ => false,
        }).AllowAnonymous();

        endpoints.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
        }).AllowAnonymous();

        return endpoints;
    }
}
