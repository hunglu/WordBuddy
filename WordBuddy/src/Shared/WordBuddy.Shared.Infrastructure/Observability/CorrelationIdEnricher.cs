using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace WordBuddy.Shared.Infrastructure.Observability;

/// <summary>
/// Enriches every log event with a <c>CorrelationId</c> property taken from the current
/// distributed-tracing <see cref="Activity"/> (its trace id), falling back to a fresh id when
/// no activity is in scope. This ties log lines to the same correlation id carried by the
/// OpenTelemetry <c>traceparent</c> header across service boundaries.
/// </summary>
public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
    }
}
