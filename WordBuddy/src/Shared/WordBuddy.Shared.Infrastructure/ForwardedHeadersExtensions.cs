using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WordBuddy.Shared.Infrastructure;

/// <summary>
/// Trusts <c>X-Forwarded-For</c> / <c>X-Forwarded-Proto</c> from configured proxy networks, so the
/// real client IP reaches rate limiting when the service runs behind Caddy / nginx / an Ingress.
/// </summary>
public static class ForwardedHeadersExtensions
{
    /// <summary>Configuration section that holds the trusted proxy networks.</summary>
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Registers <see cref="ForwardedHeadersOptions"/> from <c>ForwardedHeaders:KnownNetworks</c> (CIDR list).
    /// An empty list means no proxy is trusted and the middleware does nothing.
    /// </summary>
    /// <exception cref="InvalidOperationException">A CIDR entry is not valid (fails at startup).</exception>
    public static IServiceCollection AddWordBuddyForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        string[] networks = configuration.GetSection($"{SectionName}:KnownNetworks").Get<string[]>() ?? [];
        List<(IPAddress Prefix, int Length)> parsed = [];

        foreach (string entry in networks.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            string[] parts = entry.Trim().Split('/');
            if (parts.Length != 2
                || !IPAddress.TryParse(parts[0], out IPAddress? prefix)
                || !int.TryParse(parts[1], out int length)
                || length < 0
                || length > (prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
            {
                throw new InvalidOperationException($"{SectionName}:KnownNetworks contains an invalid CIDR entry.");
            }

            parsed.Add((prefix, length));
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = parsed.Count == 0
                ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            if (parsed.Count == 0)
            {
                return;
            }

            // Only the configured networks are trusted; the loopback defaults are removed.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach ((IPAddress prefix, int length) in parsed)
            {
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, length));
            }

            options.ForwardLimit = null;
        });

        return services;
    }

    /// <summary>Adds the forwarded-headers middleware. Call it first in the pipeline.</summary>
    public static IApplicationBuilder UseWordBuddyForwardedHeaders(this IApplicationBuilder app) =>
        app.UseForwardedHeaders();
}
