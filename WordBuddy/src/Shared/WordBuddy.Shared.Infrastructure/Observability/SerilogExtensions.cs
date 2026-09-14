using Microsoft.Extensions.Hosting;
using Serilog;

namespace WordBuddy.Shared.Infrastructure.Observability;

/// <summary>Wires Serilog as the logging backend for <see cref="Microsoft.Extensions.Logging.ILogger{T}"/>.</summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog from the host's <c>Serilog</c> configuration section (sinks, levels — see
    /// appsettings.json), enriching every log entry with <c>CorrelationId</c>, <c>ServiceName</c>,
    /// and <c>Environment</c>. Call this before <c>builder.Build()</c>.
    /// </summary>
    public static IHostBuilder ConfigureWordBuddySerilog(this IHostBuilder hostBuilder, string serviceName)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.With<CorrelationIdEnricher>()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
        });
    }
}
