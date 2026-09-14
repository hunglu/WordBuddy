using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WordBuddy.Shared.Infrastructure.Observability;

/// <summary>Wires OpenTelemetry distributed tracing (ASP.NET Core, outbound HTTP, EF Core) exported to Jaeger via OTLP.</summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Instruments inbound HTTP requests, outbound <see cref="HttpClient"/> calls, and EF Core
    /// queries, exporting traces via OTLP. The exporter endpoint comes from
    /// <c>OpenTelemetry:OtlpEndpoint</c> in configuration, falling back to the standard
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable (or the OTel SDK default,
    /// <c>http://localhost:4317</c>) when unset — Jaeger's default OTLP/gRPC port in local dev.
    /// </summary>
    public static IServiceCollection AddWordBuddyOpenTelemetry(this IServiceCollection services, string serviceName, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        string? endpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                        if (!string.IsNullOrWhiteSpace(endpoint))
                        {
                            options.Endpoint = new Uri(endpoint);
                        }
                    });
            });

        return services;
    }
}
